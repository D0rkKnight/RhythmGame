using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Calibrator : MonoBehaviour
{
    public TMP_Text instructionsText;
    public string audioSyncText = "Press any key to the sound";
    public string visualSyncText = "Press any key to the icon's pulse";

    public GameObject indicator;
    private AudioSource audio;

    public float bpm = 90;
    private float timePerPulse; // In seconds
    private float nextPulse;

    // Start is called before the first frame update
    void Start()
    {
        audio = GetComponent<AudioSource>();

        instructionsText.text = audioSyncText;

        timePerPulse = 60f / bpm;
        nextPulse = Time.time + timePerPulse;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > nextPulse)
        {
            nextPulse += timePerPulse;

            audio.Play(); // Play thump sound
        }

        // Calculate hit delta on key down
        if (Input.anyKeyDown)
        {
            float delta = Time.time - nextPulse;

            // Set to positive if closer (+ is lag, - is too early)
            if (delta < -timePerPulse / 2)
                delta += timePerPulse;

            Debug.Log(delta);
        }
    }
}
