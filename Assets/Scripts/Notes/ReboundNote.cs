using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReboundNote : Note
{
    public float reboundDelta = 1.0f;
    public int rebounds = 1;
    public int currRebound = 0; // 0 means before the initial hit
    public float reboundHeight = 3.0f;

    public override void updateNote(MusicPlayer mp, List<Note> passed)
    {
        if (currRebound == 0)
        {
            base.updateNote(mp, passed);
            return;
        }

        // Hittime is always referring to the next rebound hit

        GameObject col = lane.gameObject;
        Vector2 tPos = col.transform.Find("TriggerBox").position;

        float dt = hitTime - mp.songTime; // Song approach for mechanical purposes 
        float lerp = dt / reboundDelta; // 1-0 range of between the two hits

        float alt = -4 * reboundHeight * (lerp - 0.5f) * (lerp - 0.5f) + reboundHeight; // I hope this is right

        Vector2 dp = -mp.dir.normalized * alt;
        Vector2 p = tPos + dp;
        transform.position = new Vector3(p.x, p.y, -1);

        // Check if strictly unhittable
        if (dt < -mp.hitWindow && !dead)
        {
            mp.miss(this);
        }

        // Sort into discard pile
        if (passed != null)
        {
            float noteExtension = getNoteExtension(mp);

            if (dt < -(mp.noteTimeout + noteExtension)) passed.Add(this);
        }
    }

    public override void hit(out bool remove)
    {
        remove = false;
        if (currRebound < rebounds)
        {
            hitTime += reboundDelta; // Important: hit time gets kicked back in runtime
            currRebound++;
        }
        else
            base.hit(out remove);
    }
}
