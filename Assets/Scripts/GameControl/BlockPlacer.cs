using UnityEngine;

public class BlockPlacer : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private LayerMask placementMask;   // ground/grid faces + blocks
    [SerializeField] private float maxPlaceDistance = 1000f;
    [SerializeField] private Color previewColor = new Color(0f, 1f, 0f, 0.4f);

    [SerializeField] private Color destroyHighlightColor = new Color(1f, 1f, 0f, 0.6f); // yellow

    [SerializeField] private AudioSource blockSound;
    [SerializeField] private AudioClip[] bombDestroyClips;
    [SerializeField] private AudioClip[] powerupDestroyClips;
    [SerializeField] private ParticleSystem[] bombDestroyParticles;
    [SerializeField] private ParticleSystem[] powerupDestroyParticles;
    [SerializeField] private ParticleSystem[] blockDestroyParticles;
    private Camera mainCamera;
    private GameObject previewInstance;
    private bool hasValidPreview;
    private Vector3 previewPosition;
    private Renderer[] previewRenderers;
    private Color validPreviewColor;
    private Color invalidPreviewColor = new Color(1f, 0f, 0f, 0.4f);
    private Vector3 prefabHalfExtents;
    private readonly Collider[] overlapResults = new Collider[16];

    // Destroy-mode targeting
    private Transform destroyTarget;
    private Renderer[] destroyTargetRenderers;
    private Color[] destroyTargetOriginalColors;

    private void Awake()
    {
        mainCamera = Camera.main;

        // Create a preview instance if possible
        if (blockPrefab != null)
        {
            previewInstance = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity, transform);
            previewInstance.name = "BlockPreview";

            // Disable physics & colliders on preview
            foreach (var col in previewInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
            var rb = previewInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            previewRenderers = previewInstance.GetComponentsInChildren<Renderer>();

            // Cache extents from the prefab's rendered bounds for overlap checks
            if (previewRenderers != null && previewRenderers.Length > 0)
            {
                Bounds combined = previewRenderers[0].bounds;
                for (int i = 1; i < previewRenderers.Length; i++)
                {
                    combined.Encapsulate(previewRenderers[i].bounds);
                }
                // Slightly shrink extents so adjacent blocks that just touch are allowed
                prefabHalfExtents = combined.extents * 0.49f;
            }

            validPreviewColor = previewColor;
            ApplyPreviewColor(validPreviewColor);

            previewInstance.SetActive(false);
        }
    }

    private void Update()
    {
        if (blockPrefab == null) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        UpdateDestroyTarget();

        if (Input.GetMouseButtonDown(0))
        {
            TryDestroyBlock();
        }
    }

    private void UpdateDestroyTarget()
    {
        ClearDestroyHighlight();

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool hitSomething;
        if (placementMask.value != 0)
        {
            hitSomething = Physics.Raycast(ray, out hit, maxPlaceDistance, placementMask);
        }
        else
        {
            hitSomething = Physics.Raycast(ray, out hit, maxPlaceDistance);
        }

        if (!hitSomething || hit.collider == null)
            return;

        if (!hit.collider.CompareTag("Block") && 
            !hit.collider.CompareTag("Bomb") && 
            !hit.collider.CompareTag("PowerUp"))
            return;

        destroyTarget = hit.collider.transform;
        destroyTargetRenderers = destroyTarget.GetComponentsInChildren<Renderer>();
        if (destroyTargetRenderers == null || destroyTargetRenderers.Length == 0)
            return;

        destroyTargetOriginalColors = new Color[destroyTargetRenderers.Length];
        for (int i = 0; i < destroyTargetRenderers.Length; i++)
        {
            var r = destroyTargetRenderers[i];
            if (r == null) continue;
            var mat = r.material;
            destroyTargetOriginalColors[i] = mat.color;
            Color c = destroyHighlightColor;
            mat.color = new Color(c.r, c.g, c.b, mat.color.a * c.a);
        }
    }

    private void ClearDestroyHighlight()
    {
        if (destroyTargetRenderers == null || destroyTargetOriginalColors == null)
            return;

        int len = Mathf.Min(destroyTargetRenderers.Length, destroyTargetOriginalColors.Length);
        for (int i = 0; i < len; i++)
        {
            var r = destroyTargetRenderers[i];
            if (r == null) continue;
            var mat = r.material;
            mat.color = destroyTargetOriginalColors[i];
        }

        destroyTarget = null;
        destroyTargetRenderers = null;
        destroyTargetOriginalColors = null;
    }

    private void PlayRandomClip(AudioClip[] clips, Vector3 position)
    {
        if (clips == null || clips.Length == 0) return;
        
        int randomIndex = Random.Range(0, clips.Length);
        AudioClip randomClip = clips[randomIndex];
        
        if (randomClip != null)
        {
            AudioSource.PlayClipAtPoint(randomClip, position);
        }
    }

    private void PlayRandomParticle(ParticleSystem[] particles, Vector3 position)
    {
        if (particles == null || particles.Length == 0) return;
        
        int randomIndex = Random.Range(0, particles.Length);
        ParticleSystem randomParticle = particles[randomIndex];
        
        if (randomParticle != null)
        {
            Instantiate(randomParticle, position, Quaternion.identity);
        }
    }

    private void TryDestroyBlock()
    {
        if (destroyTarget == null)
            return;

        // Cache target before ClearDestroyHighlight() resets references
        Transform target = destroyTarget;
        ClearDestroyHighlight();
        if (target != null)
        {
            LevelManager levelManager = FindObjectOfType<LevelManager>();

            // Block deletion is disabled while paused, out of moves, timer out, or game over
            if (levelManager != null)
            {
                if (levelManager.isPaused || levelManager.isGameOver ||
                    levelManager.timerRemaining <= 0 || levelManager.movesRemaining <= 0)
                {
                    return;
                }
            }

            // Determine special behavior based on tag
            bool isBomb = target.CompareTag("Bomb");
            bool isPowerup = target.CompareTag("PowerUp");

            Vector3 hitPosition = target.position;
            
            // Play appropriate random audio clip
            if (isBomb && bombDestroyClips != null && bombDestroyClips.Length > 0)
            {
                PlayRandomClip(bombDestroyClips, hitPosition);
                PlayRandomParticle(bombDestroyParticles, hitPosition);
            }
            else if (isPowerup && powerupDestroyClips != null && powerupDestroyClips.Length > 0)
            {
                PlayRandomClip(powerupDestroyClips, hitPosition);
                PlayRandomParticle(powerupDestroyParticles, hitPosition);
            }
            else
            {
                // Fallback to default block sound for regular blocks or if no specific clips assigned
                if (blockSound != null && blockSound.clip != null)
                {
                    AudioSource.PlayClipAtPoint(blockSound.clip, hitPosition);
                    PlayRandomParticle(blockDestroyParticles, hitPosition);
                }
            }

            Destroy(target.gameObject);

            if (levelManager != null)
            {
                // Every player delete uses exactly one move
                levelManager.UseMove();

                if (isBomb)
                {
                    levelManager.OnBombDestroyed(hitPosition);
                }
                else if (isPowerup)
                {
                    levelManager.OnPowerupDestroyed();
                }
            }
        }
    }

    private void ApplyPreviewColor(Color color)
    {
        if (previewRenderers == null) return;

        foreach (var r in previewRenderers)
        {
            if (r == null || r.sharedMaterial == null) continue;
            var mat = r.material;
            Color c = mat.color;
            mat.color = new Color(color.r, color.g, color.b, c.a * color.a);
        }
    }
}
