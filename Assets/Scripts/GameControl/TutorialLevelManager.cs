using UnityEngine;

public class TutorialLevelManager : LevelManager
{
    // Tutorial-specific initialization can be added here
    void Start()
    {
        // Reset time scale to ensure game starts normally
        Time.timeScale = 1f;
        
        // Call base Start() if needed
        // base.Start();
    }
}
