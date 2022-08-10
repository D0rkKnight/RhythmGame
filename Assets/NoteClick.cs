using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteClick : MonoBehaviour, Clickable
{
    public Note parent; // Is written to externally

    public int onClick(int code)
    {
        if (MapEditor.sing == null)
            return 1;

        // If map editor is active, scour map editor for parent phrase
        foreach (BeatRow row in MapEditor.sing.workspace.rows)
        {
            foreach (BeatEditorSlot slot in row.slots)
            {
                if (slot.phrase.hardEquals(parent.phrase))
                {
                    slot.select();
                    return 1;
                }
            }
        }

        return 1;
    }

    public int onOver()
    {
        return 1;
    }
}
