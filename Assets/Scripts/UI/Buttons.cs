using UnityEngine;
using UnityEngine.SceneManagement;
public class Buttons : MonoBehaviour
{
    public void ExitGame()
    {
        Application.Quit();
    }

    public void Play()
    {
        SceneManager.LoadScene("Level 1");
    }

    public void Tutorial()
    {
        SceneManager.LoadScene("Tutorial");
    }
}
