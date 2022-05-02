using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeatRow : MonoBehaviour
{
    public Text txt;
    public int beat;
    private List<BeatEditorSlot> slots;

    // Start is called before the first frame update
    void Start()
    {
        // Collect slots from children
        slots = new List<BeatEditorSlot>();
        foreach (BeatEditorSlot b in transform.GetComponentsInChildren<BeatEditorSlot>())
            slots.Add(b);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setData(int beat_)
    {
        this.beat = beat_;
        txt.text = ""+beat_;
    }

    internal string serialize()
    {
        string o = "";

        o += "|";

        // Add row data
        for (int i=0; i<slots.Count; i++)
        {
            BeatEditorSlot slot = slots[i];
            o += slot.serialize();
            
            if (i < slots.Count-1) o += "\n";
        }

        return o;
    }
}
