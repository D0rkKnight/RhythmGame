using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZigzagPhrase : StreamPhrase
{
    public ZigzagPhrase(int lane_, float beat_, int accent_, string[] _meta) :
    base(lane_, beat_, accent_, TYPE.ZIGZAG, _meta, 4)
    {
    }

    public override Phrase clone()
    {
        return new ZigzagPhrase(lane, beat, accent, (string[]) meta.Clone());
    }

    int spawnDir; // Used during note generation

    public override void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        spawnDir = Mathf.RoundToInt(Mathf.Sign(width));
        base.spawn(mp, spawnLane, spawnBeat, blockFrame);
    }
    public override int streamNextLane(int currLane, MusicPlayer mp, int spawnLane, int endLane, float spawnBeat, float blockFrame)
    {
        // If 1 column case, don't move anything
        if (spawnLane == endLane) return currLane;

        // Shift lane
        currLane += spawnDir;
        if (currLane == spawnLane || currLane == endLane) spawnDir *= -1; // Bounce at edges

        return currLane;
    }
}
