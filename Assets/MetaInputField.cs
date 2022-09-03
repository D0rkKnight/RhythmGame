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

    public enum TEXT_DATA
    {
        RAW, INT, FLOAT
    }

    // Bunch of potential inputs
    public TMP_InputField field;
    private string fieldText;
    public TEXT_DATA fieldType;

    public Toggle toggle;

    // Keep it a string since serialization is stringwise anyways
    public string value
    {
        get
        {
            if (field != null)
                return fieldText;

            if (toggle != null)
                return toggle.isOn ? "T" : "F";

            return null; // Unlinked 
        }

        set
        {
            if (field != null)
            {
                field.text = value;
                fieldText = value; // Needs to set both
            }

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
        // Write field data into field text
        if (field != null)
        {
            bool succ = true;

            // Check if field is valid
            switch (fieldType)
            {
                case TEXT_DATA.RAW:
                    succ = true;
                    break;
                case TEXT_DATA.INT:
                    succ = int.TryParse(field.text, out int iRes);
                    break;
                case TEXT_DATA.FLOAT:
                    succ = float.TryParse(field.text, out float fRes);
                    break;
            }

            if (succ || field.text.Length == 0)
                fieldText = field.text;
            else
                field.text = fieldText; // Revert
        }
    }
}
