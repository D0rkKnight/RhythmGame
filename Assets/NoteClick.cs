using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteClick : MonoBehaviour, Clickable
{
    public Note parent; // Is written to externally
    public BeatEditorSlot parSlot = null; // Discovered on click

    public int onClick(int code)
    {
        if (MusicPlayer.sing.state == MusicPlayer.STATE.RUN)
            return 1; // Can't interact if running

        if (MapEditor.sing == null)
            return 1;

        if (MapEditor.sing.InteractMode != MapEditor.MODE.EDIT)
            return 1;

        // If map editor is active, scour map editor for parent phrase
        foreach (BeatRow row in MapEditor.sing.workspace.rows)
        {
            foreach (BeatEditorSlot slot in row.slots)
            {
                if (slot.phrase.hardEquals(parent.phrase))
                {
                    parSlot = slot;
                    slot.select();

                    MapEditor.sing.dragCol = parent.col;
                    MapEditor.sing.dragBeatOffset = parent.col.getMBeatRounded() - parent.phrase.beat;
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
