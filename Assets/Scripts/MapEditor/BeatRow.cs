using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeatRow : MonoBehaviour
{
    public TMPro.TMP_Text txt;
    public int rowNum;
    public List<BeatEditorSlot> slots;

    public GameObject editorSlotPrefab;


    // Start is called before the first frame update
    void Awake()
    {
        // Collect slots from children
        slots = new List<BeatEditorSlot>();
        foreach (BeatEditorSlot b in transform.GetComponentsInChildren<BeatEditorSlot>())
            slots.Add(b);
    }

    private void Update()
    {
    }

    public void setPhrase(Phrase p)
    {
        slots[0].setPhrase(p);
        txt.text = p.beat.ToString();
    }

    public void addPhrase(Phrase p)
    {
        GameObject slotObj = Instantiate(editorSlotPrefab, transform);
        slotObj.transform.position = slots[slots.Count - 1].transform.position + Vector3.right * 2;

        BeatEditorSlot slot = slotObj.GetComponent<BeatEditorSlot>();
        slot.setPhrase(p);

        slots.Add(slot);
    }

    internal string serialize()
    {
        string o = "";

        // Add row data
        for (int i=0; i<slots.Count; i++)
        {
            BeatEditorSlot slot = slots[i];
            o += slot.serialize();
            
            if (i < slots.Count-1) o += "\n";
        }

        return o;
    }

    public void removeSelf()
    {
        MapEditor.sing.removePhraseEntry(this);
    }
}
