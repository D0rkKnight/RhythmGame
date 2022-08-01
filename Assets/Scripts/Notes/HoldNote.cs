using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldNote : Note
{
    public float holdBeats = 4;
    public bool held = false;
    public Transform bg;

    public override void highlight(Color c)
    {
        base.highlight(c);
        bg.GetComponent<SpriteRenderer>().color = c;
    }

    protected override float getNoteExtension(MusicPlayer mp)
    {
        return holdBeats * mp.beatInterval; ;
    }
}
