using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Indicates where a note could be (used as rebound targets)
public class GhostNote : Note
{
    public override void resetInit(MusicPlayer mp)
    {
        base.resetInit(mp);
        dead = true; // Always dead
    }
}
