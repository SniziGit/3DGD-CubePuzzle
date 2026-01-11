using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelCreator : MonoBehaviour
{
    [Header("Level Variables")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private int length = 10;
    [SerializeField] private float density = 0.7f;
    [SerializeField] private int bombCount = 3;
    [SerializeField] private int powerUpCount = 3;
    [SerializeField] private int timerDuration = 120;
    [SerializeField] private int initialMoves = 50;

    [Header("Cube Preview")]
    [SerializeField] private GameObject cubePreview;

    [Header("Power-up Settings")]
    [SerializeField] private float bombRadius = 2f;
    [SerializeField] private int powerUpMoves = 6;

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI widthText;
    [SerializeField] private TextMeshProUGUI heightText;
    [SerializeField] private TextMeshProUGUI lengthText;
    [SerializeField] private TextMeshProUGUI densityText;
    [SerializeField] private TextMeshProUGUI bombCountText;
    [SerializeField] private TextMeshProUGUI powerUpCountText;
    [SerializeField] private TextMeshProUGUI timerDurationText;
    [SerializeField] private TextMeshProUGUI initialMovesText;
    [SerializeField] private TextMeshProUGUI bombRadiusText;
    [SerializeField] private TextMeshProUGUI powerUpMovesText;

    [Header("Variable Limits")]
    [SerializeField] private int minDimension = 5;
    [SerializeField] private int maxDimension = 20;
    [SerializeField] private float minDensity = 0.1f;
    [SerializeField] private float maxDensity = 1.0f;
    [SerializeField] private int minCount = 0;
    [SerializeField] private int maxCount = 20;
    [SerializeField] private int minTimer = 30;
    [SerializeField] private int maxTimer = 600;
    [SerializeField] private int minMoves = 10;
    [SerializeField] private int maxMoves = 200;
    [SerializeField] private float minBombRadius = 0.5f;
    [SerializeField] private float maxBombRadius = 5f;
    [SerializeField] private int minPowerUpMoves = 1;
    [SerializeField] private int maxPowerUpMoves = 20;

    [Header("Adjustment Values")]
    [SerializeField] private int dimensionStep = 1;
    [SerializeField] private float densityStep = 0.1f;
    [SerializeField] private int countStep = 1;
    [SerializeField] private int timerStep = 10;
    [SerializeField] private int movesStep = 5;
    [SerializeField] private float bombRadiusStep = 0.5f;
    [SerializeField] private int powerUpMovesStep = 1;

    private void Awake()
    {
        UpdateUI();
        UpdateCubeScale();
    }
    // Public methods for UI buttons to call
    public void NextToGame()
    {
        TransferVariablesToGameManager();
        LoadGameScene();
    }

    // Variable increase/decrease methods
    public void IncreaseWidth() => AdjustWidth(dimensionStep);
    public void DecreaseWidth() => AdjustWidth(-dimensionStep);
    public void IncreaseHeight() => AdjustHeight(dimensionStep);
    public void DecreaseHeight() => AdjustHeight(-dimensionStep);
    public void IncreaseLength() => AdjustLength(dimensionStep);
    public void DecreaseLength() => AdjustLength(-dimensionStep);
    public void IncreaseDensity() => AdjustDensity(densityStep);
    public void DecreaseDensity() => AdjustDensity(-densityStep);
    public void IncreaseBombCount() => AdjustBombCount(countStep);
    public void DecreaseBombCount() => AdjustBombCount(-countStep);
    public void IncreasePowerUpCount() => AdjustPowerUpCount(countStep);
    public void DecreasePowerUpCount() => AdjustPowerUpCount(-countStep);
    public void IncreaseTimerDuration() => AdjustTimerDuration(timerStep);
    public void DecreaseTimerDuration() => AdjustTimerDuration(-timerStep);
    public void IncreaseInitialMoves() => AdjustInitialMoves(movesStep);
    public void DecreaseInitialMoves() => AdjustInitialMoves(-movesStep);
    public void IncreaseBombRadius() => AdjustBombRadius(bombRadiusStep);
    public void DecreaseBombRadius() => AdjustBombRadius(-bombRadiusStep);
    public void IncreasePowerUpMoves() => AdjustPowerUpMoves(powerUpMovesStep);
    public void DecreasePowerUpMoves() => AdjustPowerUpMoves(-powerUpMovesStep);

    private void TransferVariablesToGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.width = width;
            GameManager.Instance.height = height;
            GameManager.Instance.length = length;
            GameManager.Instance.density = density;
            GameManager.Instance.bombCount = bombCount;
            GameManager.Instance.powerUpCount = powerUpCount;
            GameManager.Instance.timerDuration = timerDuration;
            GameManager.Instance.initialMoves = initialMoves;
            GameManager.Instance.SetBombRadius(bombRadius);
            GameManager.Instance.SetPowerUpMoves(powerUpMoves);

            Debug.Log("Level variables transferred to GameManager successfully!");
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene("Game");
    }

    // Variable adjustment methods
    private void AdjustWidth(int amount)
    {
        width = Mathf.Clamp(width + amount, minDimension, maxDimension);
        UpdateUI();
        UpdateCubeScale();
    }

    private void AdjustHeight(int amount)
    {
        height = Mathf.Clamp(height + amount, minDimension, maxDimension);
        UpdateUI();
        UpdateCubeScale();
    }

    private void AdjustLength(int amount)
    {
        length = Mathf.Clamp(length + amount, minDimension, maxDimension);
        UpdateUI();
        UpdateCubeScale();
    }

    private void AdjustDensity(float amount)
    {
        density = Mathf.Clamp(density + amount, minDensity, maxDensity);
        UpdateUI();
    }

    private void AdjustBombCount(int amount)
    {
        bombCount = Mathf.Clamp(bombCount + amount, minCount, maxCount);
        UpdateUI();
    }

    private void AdjustPowerUpCount(int amount)
    {
        powerUpCount = Mathf.Clamp(powerUpCount + amount, minCount, maxCount);
        UpdateUI();
    }

    private void AdjustTimerDuration(int amount)
    {
        timerDuration = Mathf.Clamp(timerDuration + amount, minTimer, maxTimer);
        UpdateUI();
    }

    private void AdjustInitialMoves(int amount)
    {
        initialMoves = Mathf.Clamp(initialMoves + amount, minMoves, maxMoves);
        UpdateUI();
    }

    private void AdjustBombRadius(float amount)
    {
        bombRadius = Mathf.Clamp(bombRadius + amount, minBombRadius, maxBombRadius);
        UpdateUI();
    }

    private void AdjustPowerUpMoves(int amount)
    {
        powerUpMoves = Mathf.Clamp(powerUpMoves + amount, minPowerUpMoves, maxPowerUpMoves);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (widthText != null) widthText.text = $"{width}";
        if (heightText != null) heightText.text = $"{height}";
        if (lengthText != null) lengthText.text = $"{length}";
        if (densityText != null) densityText.text = $"{density:F1}";
        if (bombCountText != null) bombCountText.text = $"{bombCount}";
        if (powerUpCountText != null) powerUpCountText.text = $"{powerUpCount}";
        if (timerDurationText != null) timerDurationText.text = $"{timerDuration}s";
        if (initialMovesText != null) initialMovesText.text = $"{initialMoves}";
        if (bombRadiusText != null) bombRadiusText.text = $"{bombRadius:F1}";
        if (powerUpMovesText != null) powerUpMovesText.text = $"{powerUpMoves}";
    }

    private void UpdateCubeScale()
    {
        if (cubePreview != null)
        {
            Vector3 newScale = new Vector3(width, height, length);
            cubePreview.transform.localScale = newScale;
        }
    }
}
