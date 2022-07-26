using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioButton : MonoBehaviour
{
    public GameObject audioObjPrefab;
    public AudioClip audClip;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            GameObject obj = Instantiate(audioObjPrefab);
            if (audClip != null) obj.GetComponent<AudioSource>().clip = audClip;
        });
    }
}
