using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReboundNote : Note
{
    public float reboundDelta = 1.0f;
    public int rebounds = 1;
    public int currRebound = 0; // 0 means before the initial hit

    private float linearExtend = 0.2f;

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
        float reboundTime = reboundDelta - dt;
        float lerp = reboundTime / reboundDelta;
        float alt;

        // Linear part
        if (lerp < linearExtend)
        {
            alt = reboundTime * mp.travelSpeed;
        }
        else if (lerp > (1 - linearExtend))
        {
            // Weird
            alt = (reboundDelta - reboundTime) * mp.travelSpeed;
        }
        else
        {
            float timeInArc = reboundTime - (reboundDelta * linearExtend);
            float arcLength = reboundDelta * (1 - linearExtend * 2);
            alt = -mp.travelSpeed / (arcLength) * timeInArc * timeInArc + mp.travelSpeed * timeInArc 
                + reboundDelta*linearExtend*mp.travelSpeed; // I hope this is right
        }

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
