using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HUDControl : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI movesText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bombCountText;
    public GameObject pausePanel;

    private void Update()
    {
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
            return;

        // Handle pause toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool newPaused = !levelManager.isPaused;
            levelManager.SetPaused(newPaused);

            if (pausePanel != null)
            {
                pausePanel.SetActive(newPaused);
            }
        }

        // Ensure pause panel reflects current state
        if (pausePanel != null)
        {
            pausePanel.SetActive(levelManager.isPaused);
        }

        // Update moves display
        if (movesText != null)
        {
            movesText.text = $"{levelManager.GetMovesRemaining()}";
        }

        // Update timer display (formatted as minutes:seconds) using integer seconds
        if (timerText != null)
        {
            int timeRemaining = levelManager.GetTimerRemaining();
            int minutes = timeRemaining / 60;
            int seconds = timeRemaining % 60;
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        // Update bomb count display
        if (bombCountText != null)
        {
            bombCountText.text = $"{ levelManager.GetBombCount()}";
        }
    }

    // Called by UI button to resume the game
    public void ResumeGame()
    {
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
            return;

        levelManager.SetPaused(false);

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    // Called by UI button to explicitly pause the game
    public void PauseGame()
    {
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
            return;

        levelManager.SetPaused(true);

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void ExitLevel()
    {
        // Load the title screen scene
        SceneManager.LoadScene("Title");
    }

    public void RestartLevel()
    {
        // Reload the main game scene
        SceneManager.LoadScene("Game");
    }
}
