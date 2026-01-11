using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public int width;
    public int length;
    public int height;

    public float density = 0.7f; // probability of wall presence

    public GameObject wall;
    public GameObject bomb;
    public GameObject powerUp;

    public int bombCount = 3; // number of bombs to spawn
    public int powerUpCount = 3;

    void Start()
    {
        // Try to get configuration from GameManager
        if (GameManager.Instance != null)
        {
            width = GameManager.Instance.width;
            height = GameManager.Instance.height;
            length = GameManager.Instance.length;
            SetLevelConfig(GameManager.Instance.density, GameManager.Instance.bombCount, GameManager.Instance.powerUpCount);
        }
        
        GenerateLevel();
    }

    public void SetLevelConfig(float newDensity, int newBombCount, int newPowerUpCount)
    {
        density = newDensity;
        bombCount = newBombCount;
        powerUpCount = newPowerUpCount;
    }

    void GenerateLevel()
    {
        // Keep track of all empty positions
        List<Vector3> emptyPositions = new List<Vector3>();

        // Loop over the grid and spawn walls, forming a hollow cube shell
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                for (int z = 0; z <= length; z++)
                {
                    Vector3 pos = new Vector3(x - width, z - height, y - length);

                    bool isShell = (x == 0 || x == width ||
                                    y == 0 || y == height ||
                                    z == 0 || z == length);

                    if (isShell)
                    {
                        // Always spawn a wall on the 6 sides to create a hollow cube
                        Instantiate(wall, pos, Quaternion.identity, transform);
                    }
                    else
                    {
                        // Interior cells use density to decide if a wall is present
                        if (Random.value > density)
                        {
                            Instantiate(wall, pos, Quaternion.identity, transform);
                        }
                        else
                        {
                            // This interior cell is empty, keep track of it
                            emptyPositions.Add(pos);
                        }
                    }
                }
            }
        }

        // --- Bomb placement ---
        // Shuffle empty positions to randomize selection
        for (int i = 0; i < emptyPositions.Count; i++)
        {
            Vector3 temp = emptyPositions[i];
            int randomIndex = Random.Range(i, emptyPositions.Count);
            emptyPositions[i] = emptyPositions[randomIndex];
            emptyPositions[randomIndex] = temp;
        }

        // Spawn bombs at unique empty positions
        int bombsToSpawn = Mathf.Min(bombCount, emptyPositions.Count);
        for (int i = 0; i < bombsToSpawn; i++)
        {
            Instantiate(bomb, emptyPositions[i], Quaternion.identity, transform);
        }

        //Spawn powerups at remaining empty positions
        int powerupsToSpawn = Mathf.Min(powerUpCount, emptyPositions.Count - bombsToSpawn);
        for (int i = bombsToSpawn; i < bombsToSpawn + powerupsToSpawn; i++)
        {
            Instantiate(powerUp, emptyPositions[i], Quaternion.identity, transform);
        }

        // Calculate the centre of the generated level
        Vector3 levelCentre = new Vector3(-width / 2f, -height / 2f, -length / 2f);
        
        //Find CubePlanetRotator and parent children to it
        CubePlanetRotator rotator = FindObjectOfType<CubePlanetRotator>();
        if (rotator != null)
        {
            //Re-parent children directly to rotator
            while (transform.childCount > 0)
            {
                Transform child = transform.GetChild(0);
                child.SetParent(rotator.transform, true);
                
                // Adjust child position so level centre aligns with rotator pivot
                child.position += rotator.transform.position - levelCentre;
            }
        }
        else
        {
            Debug.LogWarning("CubePlanetRotator not found in scene!");
        }
    }
}
