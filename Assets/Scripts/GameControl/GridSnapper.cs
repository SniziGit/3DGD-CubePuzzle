using UnityEngine;

[ExecuteAlways]
public class GridSnapper : MonoBehaviour
{
    [SerializeField] private bool snapPosition = true;
    [SerializeField] private bool fitToCell = false;
    [SerializeField] private bool onlyAffectXZScale = true;
    [SerializeField] private bool runInPlayMode = false; // default: snap once on enable in play mode
    [SerializeField] private bool runInEditMode = true;
    [SerializeField] private bool parentToGrid = false; // make snapped objects behave like children of the grid

    private Renderer cachedRenderer;

    private void OnEnable()
    {
        cachedRenderer = GetComponentInChildren<Renderer>();
        TrySnapAndFit();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && runInEditMode)
        {
            TrySnapAndFit();
        }
    }
#endif

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (runInPlayMode)
                TrySnapAndFit();
        }
        else
        {
            if (runInEditMode)
                TrySnapAndFit();
        }
    }

    private void TrySnapAndFit()
    {
        // Prefer an existing parent GridManager if present to avoid hopping between grids
        var existingGrid = GetComponentInParent<GridManager>();
        var ff = existingGrid != null ? existingGrid : GridManager.GetClosestGrid(transform.position);
        if (ff == null) return;

        if (parentToGrid && transform.parent == null)
        {
            // Only set the parent once; do not change it later to avoid zipping across faces
            transform.SetParent(ff.transform, true);
        }

        if (parentToGrid && transform.parent != ff.transform)
        {
            // Keep world position but parent under the grid so it rotates with it
            transform.SetParent(ff.transform, true);
        }

        if (snapPosition)
        {
            Vector3 p = transform.position;
            Vector3 snapped = ff.SnapToCellCenter(p);
            transform.position = new Vector3(snapped.x, p.y, snapped.z);
        }

        if (fitToCell)
        {
            if (cachedRenderer == null)
                cachedRenderer = GetComponentInChildren<Renderer>();
            if (cachedRenderer == null) return;

            Bounds b = cachedRenderer.bounds;
            Vector2 xz = new Vector2(b.size.x, b.size.z);
            float s = ff.GetUniformScaleToFitXZ(xz);

            Vector3 scale = transform.localScale;
            if (onlyAffectXZScale)
                transform.localScale = new Vector3(scale.x * s, scale.y, scale.z * s);
            else
                transform.localScale = scale * s;
        }
    }
}
