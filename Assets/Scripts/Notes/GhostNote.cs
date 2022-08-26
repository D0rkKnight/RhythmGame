using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Indicates where a note could be (used as rebound targets)
public class GhostNote : Note
{
    public GhostNote prev = null;
    public GhostNote next = null;
    public bool stable = false; // Determines if it is part of a stable rebound structure
    public ReboundNote reb = null; // Points to the rebound note if it is the adjacent ghost note

    // Soft reset, mantains note context
    public override void resetEnv(MusicPlayer mp)
    {
        base.resetEnv(mp);

        dead = true; // Always dead
    }

    // Full reset
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

        // Search backwards for rebound to call for recalc
        GhostNote n = this;
        while (n.prev != null)
            n = n.prev;

        if (n.reb != null)
            n.reb.calcRebounds();

        // Search forwards and mark all ghosts as unstable
        n = this;
        while (n.next != null)
        {
            n.stable = false;
            n = n.next;
        }
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

            if (rebounds > 300)
            {
                Debug.LogError("next" + next);
                Debug.LogError("prev" + prev);
                throw new System.Exception("Loop in rebound structure");
            }
        }

        n = this;
        while (n.prev != null) {
            n = n.prev;
            rebounds++;
            n.stable = true;

            if (rebounds > 300)
                throw new System.Exception("Loop in rebound structure");
        }

        stable = true; // Stabilize this note aswell

        GhostNote succ = n.next; // Grab the successor before delinking n fully so n doesn't propagate a destabilizing signal
        if (prev != null)
            prev.next = null;
        if (next != null)
            next.prev = null;
        n.next = null;
        n.prev = null;

        // n is now the earliest note in the chain
        phrase.spawn(MusicPlayer.sing, n.col.colNum, n.beat, n.blockDur, n.weight, (MusicPlayer mp) =>
        {
            // Check if the trailing ghost note is still there - may have been destroyed during collision
            if (rebounds > 0 && succ != null)
            {
                ReboundNote reb = (ReboundNote) phrase.instantiateNote(mp.reboundPrefab);
                reb.rebounds = rebounds;
                reb.reboundDelta = ((ReboundPhrase)phrase).reboundBeatDist * mp.beatInterval;

                // Link it up (succ has to exist)
                succ.reb = reb;
                reb.next = succ;

                return reb;
            } else
            {
                Note n = phrase.instantiateNote(mp.notePrefab);

                return n;
            }
        });
    }
}
