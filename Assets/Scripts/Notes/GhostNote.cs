using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Indicates where a note could be (used as rebound targets)
public class GhostNote : Note
{
    public Note parent;

    public override void resetInit(MusicPlayer mp)
    {
        base.resetInit(mp);
        dead = true; // Always dead
    }

    public override void blocked()
    {
        base.blocked();

        parent.childBlocked(this);
    }
}
