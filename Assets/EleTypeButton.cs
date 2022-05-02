using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EleTypeButton : MonoBehaviour
{
    public MapEditor.ELEMENT ele;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(setElement);
    }

    private void setElement()
    {
        MapEditor.sing.activeEle = ele;
    }
}
