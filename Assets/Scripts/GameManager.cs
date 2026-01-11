using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Configuration")]
    public int width = 10;
    public int height = 10;
    public int length = 10;
    public float density = 0.7f;
    public int bombCount = 3;
    public int powerUpCount = 3;
    public int timerDuration = 120; // seconds

    [Header("Power-up Settings")]
    [SerializeField] private float bombRadius = 2f;
    [SerializeField] private int powerUpMoves = 6;

    [Header("Game State")]
    public int initialMoves = 50;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Level configuration is now handled by LevelGenerator.Start()
        // This prevents timing issues when restarting levels
    }

    // Public getters and setters for bomb radius and power-up moves
    public float GetBombRadius() { return bombRadius; }
    public void SetBombRadius(float radius) { bombRadius = radius; }
    public int GetPowerUpMoves() { return powerUpMoves; }
    public void SetPowerUpMoves(int moves) { powerUpMoves = moves; }
}
