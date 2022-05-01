using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Reference: https://www.youtube.com/watch?v=9gAHZGArDgU

public class TrackPlayer : MonoBehaviour
{

    // Singleton
    public static TrackPlayer sing;
    private AudioSource audio;

    public AudioClip clip;
    public bool clipLoading = false;

    // Start is called before the first frame update
    void Start()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        audio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playTrack(string fname_)
    {
        clipLoading = true;
        string fname = fname_;
        string path = "file://" + Application.streamingAssetsPath + "/Tracks/";

        StartCoroutine(loadClip(fname, path));
    }

    public void resetTrack()
    {
        audio.time = 0;
    }

    private IEnumerator loadClip(string fname, string path)
    {
        WWW request = audioFromFile(path, fname);
        yield return request;

        clip = request.GetAudioClip();
        clip.name = fname;

        // Play the audio on completion
        audio.clip = clip;
        audio.Play();

        clipLoading = false;
    }

    public WWW audioFromFile(string path, string fname)
    {
        string audioToLoad = string.Format(path + "{0}", fname);
        WWW request = new WWW(audioToLoad);
        return request;
    }
}
