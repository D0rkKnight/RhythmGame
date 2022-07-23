using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Calibrator : MonoBehaviour
{
    public TMP_Text instructionsText;
    public TMP_Text deltaText;
    public Button cancelBut;

    public string audioSyncText = "Press any key to the sound";
    public string visualSyncText = "Press any key to the icon's pulse";

    public GameObject indicator;
    private AudioSource audio;

    public float bpm = 90;
    private float timePerPulse; // In seconds
    private float nextPulse;

    public float indicatorPressScale = 1.5f;
    public float aveDelta = 0f;
    public int samples = 0;

    // Start is called before the first frame update
    void Start()
    {
        audio = GetComponent<AudioSource>();

        instructionsText.text = audioSyncText;
        deltaText.text = "";

        timePerPulse = 60f / bpm;
        nextPulse = Time.time + timePerPulse;

        cancelBut.onClick.AddListener(() =>
        {
            TrackPlayer.sing.latency = aveDelta;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > nextPulse)
        {
            nextPulse += timePerPulse;

            audio.Play(); // Play thump sound
        }

        // Calculate hit delta on key down (dont read clicks or escape)
        bool mouseOrEsc = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetKey(KeyCode.Escape);
        if (Input.anyKeyDown && !mouseOrEsc)
        {
            float delta = Time.time - nextPulse;

            // Set to positive if closer (+ is lag, - is too early)
            if (delta < -timePerPulse / 2)
                delta += timePerPulse;

            samples++;
            aveDelta = (aveDelta * (samples-1) + delta) / samples;

            deltaText.text = "Offset: " + aveDelta;

            // Pulse icon
            indicator.transform.localScale = new Vector3(indicatorPressScale, indicatorPressScale, 1);
        }

        // Lerp down indicator
        indicator.transform.localScale = Vector3.Lerp(indicator.transform.localScale, Vector3.one, Time.deltaTime * 4);
    }
}
