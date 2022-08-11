using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColumnHover : MonoBehaviour, Clickable
{
    public NoteColumn parent;

    public int onClick(int code)
    {
        return 1; // pass through
    }

    public int onOver()
    {
        MapEditor me = MapEditor.sing;

        // On active and detected delta
        if (me != null
            && me.dragCol != null
            && me.selectedPhraseSlot != null)
        {
            Phrase p = me.selectedPhraseSlot.phrase.fullClone();

            // Assign as selected phrase
            if (me.dragCol != parent)
            {
                me.dragCol = parent;

                // Move active phrase as well if selection exists
                p.lane = parent.colNum;
            }

            // Check up and down drag
            MusicPlayer mp = MusicPlayer.sing;

            Vector2 delta = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - 
                parent.transform.Find("TriggerBox").position);
            float dist = Vector2.Dot(delta, -mp.dir);

            float mBeat = mp.getCurrBeat() + (dist / mp.travelSpeed / mp.beatInterval);

            // Round to quarter beat
            float rBeat = Mathf.Round(mBeat * 4) / 4;
            p.beat = rBeat;

            me.setActivePhrase(p);
        }

        return 1;
    }
}
