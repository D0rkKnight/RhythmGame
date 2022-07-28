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

        BeatEditorSlot slot = slotObj.GetComponent<BeatEditorSlot>();
        slot.setPhrase(p);

        addSlot(slot);
    }

    public void addSlot(BeatEditorSlot slot)
    {
        slots.Add(slot);
        slot.subSlot(this);

        regenSlots();
    }

    internal void serialize(List<string> data)
    {
        // Add row data
        for (int i=0; i<slots.Count; i++)
        {
            BeatEditorSlot slot = slots[i];
            data.Add(slot.serialize());
        }
    }

    public void removeSelf()
    {
        MapEditor.sing.removePhraseEntry(this);
    }

    public void desIfEmpty()
    {
        if (slots.Count == 0)
        {
            MapEditor.sing.phraseEntries.Remove(this);
            Destroy(gameObject);
        }
    }

    public void regenSlots()
    {
        for (int i=0; i<slots.Count; i++)
            slots[i].transform.localPosition = (i + 2) * Vector3.right + (i * 0.1f) * Vector3.back; // Later slots are farther forwards as well
    }
}
