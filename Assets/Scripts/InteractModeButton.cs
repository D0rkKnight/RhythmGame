using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InteractModeButton : MonoBehaviour
{
    public MapEditor.MODE mode;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            MapEditor.sing.InteractMode = mode;
        });
    }
}
