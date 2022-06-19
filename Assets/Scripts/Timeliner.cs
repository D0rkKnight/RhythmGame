using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timeliner : MonoBehaviour
{

    public GameObject cloak;

    // Config
    public bool cloakedStart = true;

    // Start is called before the first frame update
    void Start()
    {
        if (cloakedStart)
        {
            cloak.SetActive(true); // Turn on cloak to begin with

            // Unveil

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
