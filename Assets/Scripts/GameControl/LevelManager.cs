using UnityEngine;

public class LevelManager : MonoBehaviour
{

    [Header("Runtime Game State")]
    public int movesRemaining;
    public int timerRemaining; // seconds
    public int bombCount;
    public int powerUpCount;

    [Header("Pause / Result State")]
    public bool isPaused;
    public bool isGameOver;
    public bool isWin;

    [Header("Gameplay Settings")]
    [SerializeField] private float bombClearRadius;
    [SerializeField] private int powerupMoveBonus;

    [System.NonSerialized] private float timerAccumulator; // for 1-second ticks

    void Start()
    {
        // Reset time scale to ensure game starts normally
        Time.timeScale = 1f;
        
        // Initialize values from GameManager
        if (GameManager.Instance != null)
        {
            movesRemaining = GameManager.Instance.initialMoves;
            timerRemaining = GameManager.Instance.timerDuration;
            bombCount = GameManager.Instance.bombCount;
            powerUpCount = GameManager.Instance.powerUpCount;
            bombClearRadius = GameManager.Instance.GetBombRadius();
            powerupMoveBonus = GameManager.Instance.GetPowerUpMoves();
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    void Update()
    {
        if (isPaused || isGameOver)
            return;

        // Decrease timer in whole seconds
        if (timerRemaining > 0)
        {
            timerAccumulator += Time.deltaTime;

            if (timerAccumulator >= 1f)
            {
                int secondsPassed = Mathf.FloorToInt(timerAccumulator);
                timerAccumulator -= secondsPassed;

                timerRemaining -= secondsPassed;
                if (timerRemaining <= 0)
                {
                    timerRemaining = 0;
                    isGameOver = true;
                    isWin = false;
                    Debug.Log("Game Over - Time's up!");
                    // TODO: Add game over logic
                }
            }
        }
    }

    public void UseMove()
    {
        if (isPaused || isGameOver)
            return;

        movesRemaining--;
        Debug.Log($"Moves remaining: {movesRemaining}");
        
        if (movesRemaining <= 0)
        {
            movesRemaining = 0;
            isGameOver = true;
            isWin = false;
            Debug.Log("Game Over - No moves remaining!");
            // TODO: Add game over logic
        }
    }

    public void OnBombDestroyed(Vector3 position)
    {
        if (isGameOver)
            return;

        // Decrease bomb count and check win condition
        bombCount--;
        if (bombCount <= 0)
        {
            bombCount = 0;
            isGameOver = true;
            isWin = true;
            Debug.Log("Win - All bombs cleared!");
            // TODO: Add win logic
        }

        // Clear surrounding wall blocks
        Collider[] hits = Physics.OverlapSphere(position, bombClearRadius);
        foreach (var col in hits)
        {
            if (col == null) continue;

            // Only clear regular blocks/walls, not other bombs or powerups
            if (col.CompareTag("Block"))
            {
                Destroy(col.gameObject);
            }
        }
    }

    public void OnPowerupDestroyed()
    {
        if (isGameOver)
            return;

        movesRemaining += powerupMoveBonus;
    }

    public int GetMovesRemaining() { return movesRemaining; }
    public int GetTimerRemaining() { return timerRemaining; }
    public int GetBombCount() { return bombCount; }
    public int GetPowerUpCount() { return powerUpCount; }

    // Helper methods for GameOverScreen
    public bool IsOutOfMoves() { return movesRemaining <= 0; }
    public bool IsOutOfTime() { return timerRemaining <= 0; }
    public bool DidPlayerWin() { return isWin; }
}
