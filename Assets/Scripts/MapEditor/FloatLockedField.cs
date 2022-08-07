using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

[RequireComponent(typeof(TMP_InputField))]
public class FloatLockedField : MonoBehaviour
{
    public float value = 0f;
    private TMP_InputField input;

    public Action<float> cb;

    // Start is called before the first frame update
    void Awake()
    {
        input = GetComponent<TMP_InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        // Only run through if there is a filled value
        if (input.text.Trim().Length > 0)
        {

            bool succ = float.TryParse(input.text, out float parse);
            if (succ)
            {
                // On change, call the cb
                if (value != parse)
                    cb(parse);
                value = parse;
            }
            else input.text = "" + value;

        }
    }

    public void setValue(float v)
    {
        value = v;
        input.text = "" + v;
    }
}
