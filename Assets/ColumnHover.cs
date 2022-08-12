using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColumnHover : MonoBehaviour, Clickable
{
    public NoteColumn parent;

    public int onClick(int code)
    {
        MapEditor me = MapEditor.sing;
        if (me != null && me.InteractMode == MapEditor.MODE.WRITE)
            me.workspaceEditor.addPhraseEntry(); // Write in the active phrase

        return 1; // pass through
    }

    public int onOver()
    {
        MapEditor me = MapEditor.sing;

        // Check up and down drag
        MusicPlayer mp = MusicPlayer.sing;

        Vector2 delta = (Camera.main.ScreenToWorldPoint(Input.mousePosition) -
            parent.transform.Find("TriggerBox").position);
        float dist = Vector2.Dot(delta, -mp.dir);

        float mBeat = mp.getCurrBeat() + (dist / mp.travelSpeed / mp.beatInterval);

        // Round to quarter beat
        float rBeat = Mathf.Round(mBeat * 4) / 4;

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
                me.dragCol = parent;

                // Move active phrase as well if selection exists
                p.lane = parent.colNum;
            }
            p.beat = rBeat;

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
