using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldNote : Note
{
    public float holdBeats = 4;
    public bool held = false;
    public Transform bg;

    public override void setColor(Color c)
    {
        base.setColor(c);
        bg.GetComponent<SpriteRenderer>().color = c;
    }

    public override bool checkMiss(MusicPlayer mp, float dt)
    {
        return base.checkMiss(mp, dt) && !held;
    }

    protected override float getNoteExtension(MusicPlayer mp)
    {
        return holdBeats * mp.beatInterval;
    }

    public override void resetEnv(MusicPlayer mp)
    {
        base.resetEnv(mp);

        held = false;
    }

    public override void onBeat(MusicPlayer mp)
    {
        base.onBeat(mp);

        // Don't give ticking when first hit
        if (Mathf.Abs(getHitTime() - mp.songTime) < mp.hitWindow) return;

        // If still within point range
        if (held && mp.songTime < getHitTime() + (holdBeats * mp.beatInterval))
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
            setColor(Color.grey);
        }
    }

    public override void hit(out bool remove)
    {
        remove = false;

        held = true;
        setColor(Color.white);
    }
}
