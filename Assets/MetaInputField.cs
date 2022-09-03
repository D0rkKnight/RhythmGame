using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class MetaInputField : MonoBehaviour
{
    // Bunch of field types that phrases can request
    public enum TYPE
    {
        TEXT, TOGGLE
    }


    // Bunch of potential inputs
    public TMP_InputField field;
    public Toggle toggle;

    // Keep it a string since serialization is stringwise anyways
    public string value
    {
        get
        {
            if (field != null)
                return field.text;

            if (toggle != null)
                return toggle.isOn ? "T" : "F";

            return null; // Unlinked 
        }

        set
        {
            if (field != null)
                field.text = value;

            if (toggle != null)
                toggle.isOn = value == "T";
        }
    }

    public TMP_Text label;
    public string Label
    {
        get
        {
            return label == null ? null : label.text;
        }
        set
        {
            if (label != null)
                label.text = value;
        }
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
