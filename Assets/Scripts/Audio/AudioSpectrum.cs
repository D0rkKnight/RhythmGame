using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Source: https://www.youtube.com/watch?v=PzVbaaxgPco
public class AudioSpectrum : MonoBehaviour
{
    public static int spectrumLen = 64;
    public static float[] spectrum;

    // Start is called before the first frame update
    void Awake()
    {
    }

    private void Start()
    {
        spectrum = new float[spectrumLen];
    }

    // Update is called once per frame
    void Update()
    {
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Hamming);

        if (spectrum != null && spectrum.Length > 0)
        {
            for (int i=0; i<spectrum.Length; i++)
            {
                spectrum[i] *= 100;
            }
        }
    }
}
