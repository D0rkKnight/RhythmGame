using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime;

public class ScatterPhrase : StreamPhrase
{
    public ScatterPhrase(int lane_, float beat_, int accent_, string[] _meta, float priority_) :
    base(lane_, beat_, accent_, TYPE.SCATTER, _meta, 4, priority_)
    {
    }

    public override Phrase clone()
    {
        return new ScatterPhrase(lane, beat, accent, (string[])meta.Clone(), priority);
    }

    System.Random rnd;

    public override List<Note> spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        rnd = new System.Random();
        base.spawn(mp, spawnLane, spawnBeat, blockFrame, weight);

        return null;
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
