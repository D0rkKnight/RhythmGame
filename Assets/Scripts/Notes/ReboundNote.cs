using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ReboundNote : Note
{
    public float reboundDelta = 1.0f; // In seconds
    public int rebounds = 1;
    public int currRebound = 0; // 0 means before the initial hit

    private float linearExtend = 0.2f;
    public float floatingHitTime;

    public List<GhostNote> ghosts = new List<GhostNote>();

    public override void updateNote(MusicPlayer mp, List<Note> passed)
    {
        if (currRebound == 0)
        {
            base.updateNote(mp, passed);
            return;
        }

        // Hittime is always referring to the next rebound hit
        Vector2 tPos = col.transform.Find("TriggerBox").position;

        float dt = getHitTime() - mp.songTime; // Song approach for mechanical purposes 
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
        // TODO: Have mp handle this
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
            floatingHitTime += reboundDelta; // Important: hit time gets kicked back in runtime
            currRebound++;
        }
        else
            base.hit(out remove);
    }

    public override void resetInit(MusicPlayer mp)
    {
        base.resetInit(mp);

        onChange(mp);
    }

    public override void onScroll(MusicPlayer mp)
    {
        base.onScroll(mp);

        onChange(mp);
    }

    private void onChange(MusicPlayer mp)
    {
        float sDelta = mp.songTime - hitTime;
        if (sDelta < 0)
        {
            currRebound = 0;
            floatingHitTime = hitTime;

            return; // Hasn't reached this phrase yet
        }

        // Set the rebound so the note is still flying
        currRebound = (int)(sDelta / reboundDelta) + 1;
        currRebound = Mathf.Min(currRebound, rebounds); // Limit the value

        floatingHitTime = hitTime + currRebound * reboundDelta; // Set the right hit time
    }

    public override float getHitTime()
    {
        return floatingHitTime;
    }

    public override void childBlocked(Note child)
    {
        base.childBlocked(child);

        GhostNote ghost = (GhostNote)child;

        int reboundNum = ghosts.IndexOf(ghost);
        if (reboundNum < 0) return; // Somehow there's a floating child out there

        for (int i = ghosts.Count - 1; i >= reboundNum; i--)
        {
            MusicPlayer.sing.removeNote(ghosts[i]);
            ghosts.RemoveAt(i);
        }

        rebounds = reboundNum; // Limit rebounds to before the blocked note

        if (rebounds == 0)
        {
            // Swap with regular note
            int laneInd = Array.IndexOf(MusicPlayer.sing.columns, col);
            phrase.spawn(MusicPlayer.sing, laneInd, beat, blockDur, weight, (MusicPlayer mp) =>
            {
                return Instantiate(mp.notePrefab);
            });
        }
    }
}
