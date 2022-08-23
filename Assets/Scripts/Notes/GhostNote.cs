using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Indicates where a note could be (used as rebound targets)
public class GhostNote : Note
{
    public GhostNote prev;
    public GhostNote next;
    public bool stable = false; // Determines if it is part of a stable rebound structure
    public ReboundNote reb = null; // Points to the rebound note if it is the adjacent ghost note

    public override void resetInit(MusicPlayer mp)
    {
        base.resetInit(mp);

        dead = true; // Always dead
    }

    public override void onRecycle()
    {
        base.onRecycle();

        prev = null;
        next = null;
        stable = false;
        reb = null;
    }

    public override void blocked()
    {
        base.blocked();

        // Delink from next and prev
        if (prev != null) 
            prev.next = null;
        if (next != null)
            next.prev = null;

        // Signal a swap on the next revision
        if (reb != null)
            reb.swapOnRev = true;
    }

    public override void onRevise()
    {
        base.onRevise();

        if (stable)
            return;

        // Count forwards and backwards to populate the segment with a rebound note
        int rebounds = 0;
        GhostNote n = this;
        while (n.next != null) {
            n = n.next;
            rebounds++;
            n.stable = true;
        }

        n = this;
        while (n.prev != null) {
            n = n.prev;
            rebounds++;
            n.stable = true;
        }

        stable = true; // Stabilize this note aswell

        // n is now the earliest note in the chain
        /*phrase.spawn(MusicPlayer.sing, n.col.colNum, n.beat, n.blockDur, n.weight, (MusicPlayer mp) =>
        {
            // Check if the trailing ghost note is still there - may have been destroyed during collision
            if (rebounds > 0 && n.next != null)
            {
                ReboundNote reb = (ReboundNote) phrase.instantiateNote(mp.reboundPrefab);
                reb.rebounds = rebounds;
                reb.reboundDelta = ((ReboundPhrase)phrase).reboundBeatDist * mp.beatInterval;

                // Link it up (n.next has to exist)
                n.next.reb = reb;

                return reb;
            } else
            {
                Note n = phrase.instantiateNote(mp.notePrefab);

                return n;
            }
        });*/
    }
}
