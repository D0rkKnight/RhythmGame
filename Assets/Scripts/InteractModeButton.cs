using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InteractModeButton : MonoBehaviour
{
    public MapEditor.MODE mode;

    private Image img;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            MapEditor.sing.InteractMode = mode;
        });

        img = GetComponent<Image>();
    }

    private void Update()
    {
        Color col = img.color;
        col.a = MapEditor.sing.InteractMode == mode ? 1.0f : 0.5f;
        img.color = col;
    }
}
