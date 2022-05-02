using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeatRow : MonoBehaviour
{
    public Text txt;
    public int rowNum;
    public List<BeatEditorSlot> slots;

    // Start is called before the first frame update
    void Awake()
    {
        // Collect slots from children
        slots = new List<BeatEditorSlot>();
        foreach (BeatEditorSlot b in transform.GetComponentsInChildren<BeatEditorSlot>())
            slots.Add(b);
    }

    public void setData(int rowNum_)
    {
        rowNum = rowNum_;
        txt.text = ""+rowNum_;
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
