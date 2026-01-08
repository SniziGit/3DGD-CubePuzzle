using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpColor : MonoBehaviour {
    private Material mat;

    public Color colorA = Color.red;   // first color
    public Color colorB = Color.blue;  // second color
    public float speed = 2f;           // how fast the colors change
    public float maxIntensity = 5f;    // how bright the glow gets

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        mat.EnableKeyword("_EMISSION"); // Enable emission keyword
    }

    void Update()
    {
        // oscillates between 0 and 1
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        float pulse = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f * maxIntensity;

        // interpolate between colorA and colorB
        Color emissionColor = Color.Lerp(colorA, colorB, t);

        // set emission color
        mat.SetColor("_EmissionColor", emissionColor* pulse);
        // oscillates between 0 and maxIntensity
        



    }
}
