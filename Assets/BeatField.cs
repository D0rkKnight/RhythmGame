using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class BeatField : MonoBehaviour
{
    public float beat = 0f;
    private TMP_InputField input; 

    // Start is called before the first frame update
    void Start()
    {
        input = GetComponent<TMP_InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        if (input.text.Trim().Length == 0) input.text = "0";

        bool succ = float.TryParse(input.text, out float parse);
        if (succ) beat = parse;
        else input.text = ""+beat;
    }
}
