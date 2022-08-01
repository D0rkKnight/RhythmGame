using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldNote : Note
{
    public float holdBeats = 4;
    public bool held = false;

    public override void highlight(Color c)
    {
        base.highlight(c);
        Transform bg = transform.Find("HoldBar");
        bg.GetComponent<SpriteRenderer>().color = c;
    }

    protected override float getNoteExtension(MusicPlayer mp)
    {
        return holdBeats * mp.beatInterval; ;
    }

    public override void reset()
    {
        base.reset();

        held = false;
    }

    public override void onBeat(MusicPlayer mp)
    {
        base.onBeat(mp);

        // Don't give ticking when first hit
        if (Mathf.Abs(hitTime - mp.songTime) < mp.hitWindow) return;

        // If still within point range
        if (held && mp.songTime < hitTime + (holdBeats * mp.beatInterval))
        {
            mp.Score += 10;
        }
    }

    public override void onLaneRelease()
    {
        base.onLaneRelease();

        if (held)
        {
            dead = true;
            highlight(Color.grey);
        }
    }
}
