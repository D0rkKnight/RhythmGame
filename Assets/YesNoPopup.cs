using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class YesNoPopup : MonoBehaviour
{
    public Button yesBut;
    public Button noBut;
    public TMP_Text textObj;
    public string text
    {
        get { return textObj.text; }
        set { textObj.text = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
