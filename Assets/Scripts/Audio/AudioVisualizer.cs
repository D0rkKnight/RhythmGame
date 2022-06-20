using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Source: https://www.youtube.com/watch?v=PzVbaaxgPco
public class AudioVisualizer : MonoBehaviour
{
    public float bias;
    public float timeStep;
    public float timeToBeat;
    public float restSmoothTime;
    public int freqBand = 0;

    private float prevAudioVal;
    private float audioVal;
    private float timer;

    protected bool isBeat;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        OnUpdate();
    }

    public virtual void OnUpdate()
    {
        prevAudioVal = audioVal;
        audioVal = AudioSpectrum.spectrum[freqBand];

        if (prevAudioVal > bias && audioVal <= bias)
            if (timer > timeStep) OnBeat();

        if (prevAudioVal <= bias && audioVal > bias)
            if (timer > timeStep) OnBeat();

        timer += Time.deltaTime;
    }

    public virtual void OnBeat()
    {
        timer = 0;
        isBeat = true;
    }
}
