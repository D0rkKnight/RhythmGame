using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime;

public class ScatterPhrase : StreamPhrase
{
    public ScatterPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, string[] _meta) :
    base(lane_, partition_, beat_, accent_, wait_, TYPE.SCATTER, _meta, 4)
    {
    }

    public override Phrase clone()
    {
        return new ScatterPhrase(lane, partition, beat, accent, wait, (string[])meta.Clone());
    }

    System.Random rnd;

    public override void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        rnd = new System.Random();
        base.spawn(mp, spawnLane, spawnBeat, blockFrame);
    }
    public override int streamNextLane(int currLane, MusicPlayer mp, int spawnLane, int endLane, float spawnBeat, float blockFrame)
    {
        // Generates random value
        int next = 0;
        if (endLane > spawnLane) next = rnd.Next(0, endLane - spawnLane);
        else if (endLane < spawnLane) next = rnd.Next(endLane - spawnLane, 0);

        return spawnLane + next;
    }
}
