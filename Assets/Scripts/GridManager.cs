using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // Primary instance for legacy callers; for multi-grid setups use AllGrids / GetClosestGrid
    public static GridManager Instance { get; private set; }
    
    // Registry of all active grids so we can support one grid per cube face
    public static readonly List<GridManager> AllGrids = new List<GridManager>();

    [Header("Grid")]
    // Local-space origin offset of the grid relative to this transform
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    [SerializeField] private int gridWidth = 64;
    [SerializeField] private int gridHeight = 64;
    [SerializeField] private float cellSize = 1f; // hex size (radius)
    [SerializeField] private bool useCircularMask = true;
    [SerializeField] private int circularRadius = -1; // <= 0 => auto-radius

    [Header("Path Targets")] 
    [SerializeField] private Transform goal;
    [SerializeField] private List<Transform> spawns = new List<Transform>();

    [Header("Collision")]
    [SerializeField] private LayerMask unwalkableMask; // obstacles/turrets/walls
    [SerializeField] private bool restrictToWalkableMask = false; // if true, only tiles on walkableMask are allowed
    [SerializeField] private LayerMask walkableMask; // lanes/ground tiles that define allowed areas
    [SerializeField] [Range(0.5f, 1.2f)] private float cellOverlapScaleXZ = 0.95f; // scale of the cell box used for overlap checks
    [SerializeField] private float cellOverlapHeight = 3f; // Y size of the overlap box used for checks

    private const int BlockedCost = 255;
    
    // Public property to access cell size
    public float CellSize => cellSize;

    private byte[] costField;
    private int[] integrationField;
    private Vector2[] flowField; // XZ plane

    // Square grid neighbors: right, up, left, down
    private readonly static Vector2Int[] neighbors4 = new Vector2Int[]
    {
        new Vector2Int(1, 0),  // right
        new Vector2Int(0, 1),  // up
        new Vector2Int(-1, 0), // left
        new Vector2Int(0, -1)  // down
    };

    [Header("Gizmos")]
    [SerializeField] private bool drawGridGizmos = true;
    [SerializeField] private bool drawFlowGizmos = true;
    [SerializeField] private Color gridColor = new Color(1f,1f,1f,0.1f);
    [SerializeField] private Color flowColor = Color.cyan;
    [SerializeField] private float arrowScale = 0.5f;
    [SerializeField] private bool drawCellStates = true;
    [SerializeField] private Color blockedColor = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private Color reachableColor = new Color(0.2f, 0.8f, 0.2f, 0.25f);
    [SerializeField] private Color unreachableColor = new Color(1f, 0.9f, 0.2f, 0.2f);
    [SerializeField] private Color goalColor = new Color(0.1f, 0.6f, 1f, 0.5f);
    [SerializeField] private Color spawnColor = new Color(0.8f, 0.3f, 1f, 0.5f);
    
    [Header("Path Visualization")]
    [SerializeField] private bool showPath = true;
    [SerializeField] private float pathHeight = 0.2f;
    [SerializeField] private Color pathColor = new Color(1f, 0.5f, 0f, 0.8f);
    [SerializeField] private float pathThickness = 0.1f;
    [SerializeField] private float pathUpdateInterval = 0.5f;
    [SerializeField] private int maxPathSegments = 100;
    private LineRenderer pathLineRenderer;
    private float nextPathUpdateTime;

    private void Awake()
    {
        // Allow multiple grid managers; keep the first one as Instance for backwards compatibility
        if (Instance == null)
        {
            Instance = this;
        }

        if (!AllGrids.Contains(this))
        {
            AllGrids.Add(this);
        }
        
        // Create and configure the path line renderer
        GameObject lineObj = new GameObject("PathLine");
        lineObj.transform.SetParent(transform, false);
        pathLineRenderer = lineObj.AddComponent<LineRenderer>();
        pathLineRenderer.startWidth = pathThickness;
        pathLineRenderer.endWidth = pathThickness;
        pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"))
        {
            renderQueue = 3000 // Make sure it renders on top of other objects
        };
        pathLineRenderer.startColor = pathColor;
        pathLineRenderer.endColor = pathColor;
        pathLineRenderer.positionCount = 0;
        pathLineRenderer.useWorldSpace = true;
        pathLineRenderer.textureMode = LineTextureMode.Tile;
        pathLineRenderer.generateLightingData = false;
        
        Allocate();
    }

    private void Start()
    {
        RebuildAll();
        nextPathUpdateTime = Time.time + pathUpdateInterval;
    }

    private void Allocate()
    {
        int n = gridWidth * gridHeight;
        if (costField == null || costField.Length != n) costField = new byte[n];
        if (integrationField == null || integrationField.Length != n) integrationField = new int[n];
        if (flowField == null || flowField.Length != n) flowField = new Vector2[n];
    }

    public void RebuildAll()
    {
        Allocate();
        BuildCostField();
        BuildIntegrationField();
        BuildFlowField();
        UpdatePathVisualization();
    }

    public void NotifyMapChanged()
    {
        RebuildAll();
    }

    /// <summary>
    /// Returns the GridManager whose snapped cell center is closest to the given world position.
    /// This allows having one grid per cube face and smoothly switching between them.
    /// </summary>
    public static GridManager GetClosestGrid(Vector3 worldPos)
    {
        GridManager best = null;
        float bestDistSq = float.PositiveInfinity;

        for (int i = 0; i < AllGrids.Count; i++)
        {
            GridManager gm = AllGrids[i];
            if (gm == null) continue;

            Vector3 snapped = gm.SnapToCellCenter(worldPos);
            float d2 = (snapped - worldPos).sqrMagnitude;
            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                best = gm;
            }
        }

        // Fallback to legacy Instance if registry is empty or all entries are null
        return best != null ? best : Instance;
    }

    public bool CanPlaceWithoutBlocking(Vector3 worldPos)
    {
        if (goal == null) return true;
        Vector2Int cell = WorldToCell(worldPos);
        if (!InBounds(cell)) return false;

        byte[] costBackup = (byte[])costField.Clone();
        int idx = ToIndex(cell);
        costField[idx] = BlockedCost;
        BuildIntegrationField();
        bool allReach = AreAllSpawnsReachable();
        costField = costBackup; // restore
        BuildIntegrationField(); // restore original integration for consistency
        return allReach;
    }

    public Vector3 GetFlowAt(Vector3 worldPos)
    {
        Vector2Int c = WorldToCell(worldPos);
        if (!InBounds(c)) return Vector3.zero;
        Vector2 v = flowField[ToIndex(c)];
        return new Vector3(v.x, 0f, v.y);
    }

    // Returns true if the given world position corresponds to a cell that is
    // inside the grid & circular mask and is not blocked in the cost field.
    public bool IsBuildableCell(Vector3 worldPos)
    {
        Vector2Int c = WorldToCell(worldPos);
        if (!InBounds(c) || !IsInsideCircularMask(c)) return false;
        int idx = ToIndex(c);
        if (costField == null || idx < 0 || idx >= costField.Length) return false;
        return costField[idx] < BlockedCost;
    }

    // Utilities for snapping/placement helpers
    public Vector3 SnapToCellCenter(Vector3 worldPos)
    {
        Vector2Int cell = WorldToCell(worldPos);
        return CellCenter(cell);
    }

    public Vector2Int WorldToCellPublic(Vector3 worldPos) => WorldToCell(worldPos);

    public Vector3 GetCellCenterPublic(Vector2Int cell) => CellCenter(cell);

    public bool TryGetBestNextCellCenter(Vector3 fromWorld, out Vector3 nextCenter)
    {
        nextCenter = Vector3.zero;
        Vector2Int c = WorldToCell(fromWorld);
        if (!InBounds(c)) return false;
        int i = ToIndex(c);
        if (integrationField == null || i < 0 || i >= integrationField.Length) return false;

        int currentVal = integrationField[i];
        int bestVal = int.MaxValue;
        Vector2Int best = c;
        for (int ni = 0; ni < neighbors4.Length; ni++)
        {
            Vector2Int nc = c + neighbors4[ni];
            if (!InBounds(nc)) continue;
            int nIndex = ToIndex(nc);
            int nVal = integrationField[nIndex];
            if (nVal < bestVal)
            {
                bestVal = nVal;
                best = nc;
            }
        }
        // Prefer any reachable neighbor; otherwise fall back to current if reachable
        if (bestVal < int.MaxValue)
        {
            nextCenter = CellCenter(best);
            return true;
        }
        if (currentVal < int.MaxValue)
        {
            nextCenter = CellCenter(c);
            return true;
        }
        return false;
    }

    public Bounds GetCellWorldBounds(Vector2Int cell)
    {
        Vector3 c = CellCenter(cell);
        Vector3 size = new Vector3(cellSize, 2f, cellSize);
        return new Bounds(c + Vector3.up * 0.5f, size);
    }

    public float GetUniformScaleToFitXZ(Vector2 xzSize)
    {
        // For square grid, both dimensions use cellSize
        float allowedSize = cellSize;
        float fx = (xzSize.x <= 0.0001f) ? 1f : allowedSize / xzSize.x;
        float fz = (xzSize.y <= 0.0001f) ? 1f : allowedSize / xzSize.y;
        return Mathf.Min(fx, fz);
    }

    private void BuildCostField()
    {
        for (int r = 0; r < gridHeight; r++)
        {
            for (int q = 0; q < gridWidth; q++)
            {
                int i = q + r * gridWidth;
                Vector2Int cell = new Vector2Int(q, r);
                if (!IsInsideCircularMask(cell))
                {
                    costField[i] = (byte)BlockedCost;
                    continue;
                }
                Vector3 center = CellCenter(cell);
                // Derive half extents from flat-top cell bounds
                Bounds b = GetCellWorldBounds(cell);
                Vector3 halfExt = 0.5f * new Vector3(b.size.x * cellOverlapScaleXZ, cellOverlapHeight, b.size.z * cellOverlapScaleXZ);
                bool blocked = Physics.CheckBox(center, halfExt, Quaternion.identity, unwalkableMask);

                // Treat a cell as walkable only if there is actual ground directly beneath its center
                // so edge cells that are "over the void" become blocked.
                bool hasWalkable = false;
                Vector3 rayStart = center + Vector3.up * (cellOverlapHeight * 0.5f);
                float rayDistance = cellOverlapHeight;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, rayDistance, walkableMask))
                {
                    hasWalkable = true;
                }

                bool allowed = !restrictToWalkableMask || hasWalkable;

                costField[i] = (!allowed || blocked || !hasWalkable) ? (byte)BlockedCost : (byte)1;
            }
        }
    }

    private void BuildIntegrationField()
    {
        int n = gridWidth * gridHeight;
        for (int i = 0; i < n; i++) integrationField[i] = int.MaxValue;
        if (goal == null) return;

        Vector2Int goalCell = WorldToCell(goal.position);
        if (!InBounds(goalCell)) return;

        int goalIndex = ToIndex(goalCell);
        integrationField[goalIndex] = 0;

        var pq = new SimpleMinHeap();
        pq.Push(goalIndex, 0);

        while (pq.Count > 0)
        {
            pq.Pop(out int current, out int currentCost);
            if (currentCost > integrationField[current]) continue;
            Vector2Int cc = FromIndex(current);
            for (int ni = 0; ni < neighbors4.Length; ni++)
            {
                Vector2Int nc = cc + neighbors4[ni];
                if (!InBounds(nc) || !IsInsideCircularMask(nc)) continue;
                int neighborIndex = ToIndex(nc);
                if (costField[neighborIndex] >= BlockedCost) continue;

                int stepCost = costField[neighborIndex];
                int newCost = currentCost + stepCost;
                if (newCost < integrationField[neighborIndex])
                {
                    integrationField[neighborIndex] = newCost;
                    pq.Push(neighborIndex, newCost);
                }
            }
        }
    }

    private void BuildFlowField()
    {
        for (int r = 0; r < gridHeight; r++)
        {
            for (int q = 0; q < gridWidth; q++)
            {
                int i = q + r * gridWidth;
                if (!IsInsideCircularMask(new Vector2Int(q, r)))
                {
                    flowField[i] = Vector2.zero;
                    continue;
                }
                if (costField[i] >= BlockedCost || integrationField[i] == int.MaxValue)
                {
                    flowField[i] = Vector2.zero;
                    continue;
                }

                int lowest = integrationField[i];
                Vector2Int best = new Vector2Int(q, r);

                for (int ni = 0; ni < neighbors4.Length; ni++)
                {
                    Vector2Int nc = new Vector2Int(q, r) + neighbors4[ni];
                    if (!InBounds(nc) || !IsInsideCircularMask(nc)) continue;
                    int nIndex = ToIndex(nc);
                    int nVal = integrationField[nIndex];
                    if (nVal < lowest)
                    {
                        lowest = nVal;
                        best = nc;
                    }
                }

                Vector3 from = CellCenter(new Vector2Int(q, r));
                Vector3 to = CellCenter(best);
                Vector3 dir = (to - from);
                dir.y = 0f;
                Vector2 flat = new Vector2(dir.x, dir.z);
                flowField[i] = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector2.zero;
            }
        }
    }

    private bool AreAllSpawnsReachable()
    {
        if (spawns == null || spawns.Count == 0) return true;
        for (int i = 0; i < spawns.Count; i++)
        {
            var s = spawns[i];
            if (s == null) continue;
            Vector2Int c = WorldToCell(s.position);
            if (!InBounds(c)) return false;
            if (integrationField[ToIndex(c)] == int.MaxValue) return false;
        }
        return true;
    }

    private bool InBounds(Vector2Int c)
    {
        return c.x >= 0 && c.y >= 0 && c.x < gridWidth && c.y < gridHeight;
    }

    private bool IsInsideCircularMask(Vector2Int cell)
    {
        if (!useCircularMask) return true;

        int centerX = (gridWidth - 1) / 2;
        int centerY = (gridHeight - 1) / 2;
        int radius = circularRadius > 0 ? circularRadius : Mathf.Min(gridWidth, gridHeight) / 2;

        // Simple circular distance check for square grid
        int dx = cell.x - centerX;
        int dy = cell.y - centerY;
        return (dx * dx + dy * dy) <= (radius * radius);
    }

    private int ToIndex(Vector2Int c) => c.x + c.y * gridWidth;

    private Vector2Int FromIndex(int index)
    {
        int r = index / gridWidth;
        int q = index - r * gridWidth;
        return new Vector2Int(q, r);
    }

    private Vector2Int WorldToCell(Vector3 world)
    {
        // Convert world position into this grid's local space, accounting for rotation and scale
        Vector3 local = transform.InverseTransformPoint(world) - gridOrigin;
        Vector3 gridSpace = Quaternion.Inverse(transform.rotation) * local;
        int x = Mathf.FloorToInt(gridSpace.x / cellSize + 0.5f);
        int y = Mathf.FloorToInt(gridSpace.z / cellSize + 0.5f);
        return new Vector2Int(
            Mathf.Clamp(x, 0, gridWidth - 1),
            Mathf.Clamp(y, 0, gridHeight - 1)
        );
    }

    private Vector3 CellCenter(Vector2Int cell)
    {
        // Convert grid coordinates into world space, respecting the object's transform
        Vector3 localPos = new Vector3(
            cell.x * cellSize,
            0f,
            cell.y * cellSize
        ) + gridOrigin;
        
        // Apply local rotation and scale, then transform to world space
        return transform.TransformPoint(localPos);
    }

    private void OnDrawGizmos()
    {
        if (!drawGridGizmos && !drawFlowGizmos && !drawCellStates) return;
        if (gridWidth <= 0 || gridHeight <= 0) return;

        Color old = Gizmos.color;
        // Draw per-cell states (blocked/reachable/unreachable)
        if (drawCellStates)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                for (int q = 0; q < gridWidth; q++)
                {
                    int i = q + r * gridWidth;
                    Vector3 c = CellCenter(new Vector2Int(q, r));
                    Color col;
                    bool isBlocked = (costField != null && i < costField.Length && costField[i] >= BlockedCost);
                    bool isReachable = (integrationField != null && i < integrationField.Length && integrationField[i] != int.MaxValue);
                    if (isBlocked) col = blockedColor;
                    else if (isReachable) col = reachableColor;
                    else col = unreachableColor;

                    Gizmos.color = col;
                    // Draw a square footprint at the same size as the logical cell
                    DrawSquare(c + Vector3.up * 0.01f, cellSize * 0.95f);
                }
            }
        }
        if (drawGridGizmos)
        {
            Gizmos.color = gridColor;
            for (int r = 0; r < gridHeight; r++)
            {
                for (int q = 0; q < gridWidth; q++)
                {
                    Vector3 c = CellCenter(new Vector2Int(q, r));
                    DrawSquare(c, cellSize);
                }
            }
        }

        if (drawFlowGizmos && flowField != null)
        {
            Gizmos.color = flowColor;
            for (int r = 0; r < gridHeight; r++)
            {
                for (int q = 0; q < gridWidth; q++)
                {
                    int i = q + r * gridWidth;
                    if (i < 0 || i >= flowField.Length) continue;
                    Vector2 v = flowField[i];
                    if (v.sqrMagnitude < 0.0001f) continue;
                    Vector3 c = CellCenter(new Vector2Int(q, r));
                    Vector3 dir = new Vector3(v.x, 0f, v.y) * cellSize * arrowScale;
                    Gizmos.DrawLine(c, c + dir);
                    Vector3 right = Quaternion.Euler(0f, 30f, 0f) * dir * 0.5f;
                    Vector3 left = Quaternion.Euler(0f, -30f, 0f) * dir * 0.5f;
                    Gizmos.DrawLine(c + dir, c + dir - right);
                    Gizmos.DrawLine(c + dir, c + dir - left);
                }
            }
        }

        // Mark goal and spawns
        if (goal != null)
        {
            Gizmos.color = goalColor;
            Vector3 gc = SnapToCellCenter(goal.position);
            Gizmos.DrawSphere(gc + Vector3.up * 0.05f, cellSize * 0.25f);
        }
        if (spawns != null)
        {
            Gizmos.color = spawnColor;
            for (int i = 0; i < spawns.Count; i++)
            {
                var s = spawns[i]; if (s == null) continue;
                Vector3 sc = SnapToCellCenter(s.position);
                Gizmos.DrawSphere(sc + Vector3.up * 0.05f, cellSize * 0.2f);
            }
        }
        // Draw path visualization in editor
        if (showPath && Application.isPlaying)
        {
            DrawPathInGizmos();
        }
        
        Gizmos.color = old;
    }

    private void DrawSquare(Vector3 center, float size)
    {
        // Draw square in local space, respecting the grid's orientation
        float halfSize = size * 0.5f;
        Vector3 localCenter = transform.InverseTransformPoint(center);
        
        Vector3 a = transform.TransformPoint(new Vector3(-halfSize, 0, -halfSize) + localCenter);
        Vector3 b = transform.TransformPoint(new Vector3(halfSize, 0, -halfSize) + localCenter);
        Vector3 c = transform.TransformPoint(new Vector3(halfSize, 0, halfSize) + localCenter);
        Vector3 d = transform.TransformPoint(new Vector3(-halfSize, 0, halfSize) + localCenter);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }


    private void UpdatePathVisualization()
    {
        if (!showPath || spawns == null || spawns.Count == 0 || goal == null)
        {
            if (pathLineRenderer != null)
                pathLineRenderer.positionCount = 0;
            return;
        }

        List<Vector3> pathPoints = new List<Vector3>();

        foreach (var spawn in spawns)
        {
            if (spawn == null) continue;
            
            // Add spawn point
            Vector3 spawnPos = spawn.position;
            spawnPos.y = pathHeight;
            pathPoints.Add(spawnPos);

            // Follow flow field from spawn to goal
            Vector2Int currentCell = WorldToCell(spawnPos);
            Vector2Int goalCell = WorldToCell(goal.position);
            
            int safetyCounter = 0;
            const int maxSteps = 1000;
            
            while (safetyCounter++ < maxSteps && InBounds(currentCell) && currentCell != goalCell)
            {
                int cellIndex = ToIndex(currentCell);
                if (cellIndex < 0 || cellIndex >= flowField.Length || flowField[cellIndex] == Vector2.zero)
                    break;

                // Get next cell based on flow field
                Vector2 flow = flowField[cellIndex];
                Vector3 worldFlow = new Vector3(flow.x, 0, flow.y);
                Vector3 nextWorldPos = CellCenter(currentCell) + worldFlow * cellSize;
                nextWorldPos.y = pathHeight;
                pathPoints.Add(nextWorldPos);

                // Move to next cell
                Vector2Int nextCell = WorldToCell(nextWorldPos);
                if (nextCell == currentCell) // Prevent infinite loops
                    break;
                    
                currentCell = nextCell;
                
                // Limit the number of points for performance
                if (pathPoints.Count >= maxPathSegments)
                    break;
            }
            
            // Add goal point
            if (InBounds(goalCell) && pathPoints.Count < maxPathSegments)
            {
                Vector3 goalPos = goal.position;
                goalPos.y = pathHeight;
                pathPoints.Add(goalPos);
            }
        }

        // Update line renderer
        if (pathLineRenderer != null && pathPoints.Count > 1)
        {
            pathLineRenderer.positionCount = pathPoints.Count;
            pathLineRenderer.SetPositions(pathPoints.ToArray());
        }
        else if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount = 0;
        }
    }

    private void DrawPathInGizmos()
    {
        if (!showPath || spawns == null || spawns.Count == 0 || goal == null) return;

        Gizmos.color = pathColor;

        foreach (var spawn in spawns)
        {
            if (spawn == null) continue;
            
            Vector3 currentPos = spawn.position;
            currentPos.y = pathHeight;
            Vector2Int currentCell = WorldToCell(currentPos);
            Vector2Int goalCell = WorldToCell(goal.position);
            
            int safetyCounter = 0;
            const int maxSteps = 1000;
            Vector3 lastPos = currentPos;
            
            while (safetyCounter++ < maxSteps && InBounds(currentCell) && currentCell != goalCell)
            {
                int cellIndex = ToIndex(currentCell);
                if (cellIndex < 0 || cellIndex >= flowField.Length || flowField[cellIndex] == Vector2.zero)
                    break;

                // Get next cell based on flow field
                Vector2 flow = flowField[cellIndex];
                Vector3 worldFlow = new Vector3(flow.x, 0, flow.y);
                Vector3 nextPos = CellCenter(currentCell) + worldFlow * cellSize;
                nextPos.y = pathHeight;
                
                // Draw line segment
                Gizmos.DrawLine(lastPos, nextPos);
                
                // Draw arrow head
                Vector3 dir = (nextPos - lastPos).normalized;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Vector3 right = Quaternion.Euler(0, 30, 0) * dir * 0.2f;
                    Vector3 left = Quaternion.Euler(0, -30, 0) * dir * 0.2f;
                    Gizmos.DrawLine(nextPos, nextPos - right);
                    Gizmos.DrawLine(nextPos, nextPos - left);
                }

                lastPos = nextPos;
                currentCell = WorldToCell(nextPos);
                
                if (safetyCounter >= maxPathSegments) break;
            }
            
            // Draw final line to goal
            if (InBounds(goalCell))
            {
                Vector3 goalPos = goal.position;
                goalPos.y = pathHeight;
                Gizmos.DrawLine(lastPos, goalPos);
            }
        }
    }

    private class SimpleMinHeap
    {
        private readonly List<int> nodes = new List<int>();
        private readonly List<int> costs = new List<int>();
        public int Count => nodes.Count;
        public void Push(int node, int cost)
        {
            nodes.Add(node);
            costs.Add(cost);
            SiftUp(Count - 1);
        }
        public void Pop(out int node, out int cost)
        {
            int last = Count - 1;
            node = nodes[0];
            cost = costs[0];
            nodes[0] = nodes[last];
            costs[0] = costs[last];
            nodes.RemoveAt(last);
            costs.RemoveAt(last);
            if (Count > 0) SiftDown(0);
        }
        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (costs[p] <= costs[i]) break;
                Swap(i, p);
                i = p;
            }
        }
        private void SiftDown(int i)
        {
            while (true)
            {
                int l = i * 2 + 1;
                int r = l + 1;
                int s = i;
                if (l < Count && costs[l] < costs[s]) s = l;
                if (r < Count && costs[r] < costs[s]) s = r;
                if (s == i) break;
                Swap(i, s);
                i = s;
            }
        }
        private void Swap(int a, int b)
        {
            int tn = nodes[a]; nodes[a] = nodes[b]; nodes[b] = tn;
            int tc = costs[a]; costs[a] = costs[b]; costs[b] = tc;
        }
    }
}
