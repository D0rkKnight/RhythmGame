using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pushes the gameobject onto the panel stack
public class PanelInitializer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameManager.sing.pushPanelStack(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
