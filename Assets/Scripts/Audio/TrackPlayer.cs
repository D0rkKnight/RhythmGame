using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Reference: https://www.youtube.com/watch?v=9gAHZGArDgU

public class TrackPlayer : MonoBehaviour
{

    // Singleton
    public static TrackPlayer sing;
    public AudioSource audio;

    public AudioClip clip;
    public bool clipLoading = false;

    public float vol = 0.2f;
    public float latency = 0f; // How many seconds behind the game state the audio lags

    // Start is called before the first frame update
    void Start()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        audio = GetComponent<AudioSource>();
        audio.volume = vol;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void loadTrack(string fname_)
    {
        clipLoading = true;
        string fname = fname_;
        string path = "file://" + Application.streamingAssetsPath + "/Tracks/";

        StartCoroutine(loadClip(fname, path));
    }

    public void play()
    {
        audio.clip = clip;
        audio.Play();
    }

    public void resetTrack()
    {
        audio.time = 0;
        audio.Stop();
    }

    private IEnumerator loadClip(string fname, string path)
    {
        WWW request = audioFromFile(path, fname);
        yield return request;

        clip = request.GetAudioClip();
        clip.name = fname;

        clipLoading = false;
    }

    public WWW audioFromFile(string path, string fname)
    {
        string audioToLoad = string.Format(path + "{0}", fname);
        WWW request = new WWW(audioToLoad);
        return request;
    }

    public void setTime(float time)
    {
        if (time + latency >= audio.clip.length) return; // Out of bounds
        audio.time = time + latency;
    }
}
