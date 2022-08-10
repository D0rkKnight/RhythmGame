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
            && me.dragCol != parent)
        {
            me.dragCol = parent;

            // Move active phrase as well if selection exists
            if (me.selectedPhraseSlot != null)
            {
                Phrase p = me.selectedPhraseSlot.phrase.fullClone();
                p.lane = parent.colNum;
                me.setActivePhrase(p);
            }
        }

        return 1;
    }
}
