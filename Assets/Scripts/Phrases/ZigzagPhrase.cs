using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZigzagPhrase : StreamPhrase
{
    public ZigzagPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, string[] _meta) :
    base(lane_, partition_, beat_, accent_, wait_, TYPE.ZIGZAG, _meta, 4)
    {
    }

    public override Phrase clone()
    {
        return new ZigzagPhrase(lane, partition, beat, accent, wait, (string[]) meta.Clone());
    }

    protected override bool genTypeBlock(out string res, List<string> meta)
    {
        base.genTypeBlock(out res, meta);

        res = "Z";
        return true;
    }

    int spawnDir; // Used during note generation

    public override void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        spawnDir = Mathf.RoundToInt(Mathf.Sign(width));
        base.spawn(mp, spawnLane, spawnBeat, blockFrame);
    }
    public override int streamNextLane(int currLane, MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        int bound = spawnLane + width - 1;

        // Shift lane
        currLane += spawnDir;
        if (currLane == spawnLane || currLane == bound) spawnDir *= -1; // Bounce at edges

        return currLane;
    }
}
