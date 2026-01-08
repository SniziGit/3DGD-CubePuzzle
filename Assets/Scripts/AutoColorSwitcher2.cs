using UnityEngine;

public class AutoColorSwitcher2 : MonoBehaviour
{
    private Material mat;
    public Color baseEmissionColor = Color.green; // base glow color
    public float maxIntensity = 5f;               // how bright the glow gets

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        mat.EnableKeyword("_EMISSION"); // Enable emission keyword
    }

    void Update()
    {
        // oscillates between 0 and maxIntensity
        float pulse = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f * maxIntensity;


        // multiply base color by intensity (HDR values allowed)
        mat.SetColor("_EmissionColor", baseEmissionColor * pulse);
    }
}
