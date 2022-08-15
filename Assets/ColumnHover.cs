using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColumnHover : MonoBehaviour, Clickable
{
    public NoteColumn parent;

    public int onClick(int code)
    {
        if (MusicPlayer.sing.state == MusicPlayer.STATE.RUN)
            return 1; // Can't interact if running

        MapEditor me = MapEditor.sing;
        if (me != null && me.InteractMode == MapEditor.MODE.WRITE)
            me.workspaceEditor.addPhraseEntry(); // Write in the active phrase

        return 1; // pass through
    }

    public int onOver()
    {
        if (MusicPlayer.sing.state == MusicPlayer.STATE.RUN)
            return 1; // Can't interact if running

        MapEditor me = MapEditor.sing;

        // Check up and down drag
        MusicPlayer mp = MusicPlayer.sing;

        // Round to quarter beat
        float rBeat = parent.getMBeatRounded();

        // On active and detected delta
        if (me != null
            && me.dragCol != null
            && me.selectedPhraseSlot != null
            && me.InteractMode == MapEditor.MODE.EDIT)
        {
            Phrase p = me.selectedPhraseSlot.phrase.hardClone();

            // Assign as selected phrase
            if (me.dragCol != parent)
            {
                // Move active phrase as well if selection exists
                // This should track left right dragging regardless of starting column
                p.lane += parent.colNum - me.dragCol.colNum;
                p.lane = Mathf.Clamp(p.lane, 0, MusicPlayer.sing.columns.Length - 1);

                me.dragCol = parent;
            }
            p.beat = rBeat - me.dragBeatOffset;

            me.setActivePhrase(p);
        }

        // If not dragging, do previz and set active phrase
        if (me != null && me.dragCol == null
            && me.InteractMode == MapEditor.MODE.WRITE)
        {
            Phrase p = me.activePhrase.fullClone();

            p.lane = parent.colNum;
            p.beat = rBeat;

            me.setActivePhrase(p);
        }

        return 1;
    }
}
