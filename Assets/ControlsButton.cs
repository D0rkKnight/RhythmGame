using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlsButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Only allow controls config if in a game environment
        // (otherwise it's not clear which profile to write the controls to)
        gameObject.SetActive(MusicPlayer.sing != null);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
