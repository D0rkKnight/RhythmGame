using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReboundPhrase : Phrase
{
    float reboundDelta = 1.0f; // Distance of rebounded note

    public ReboundPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, string[] typeMeta_) :
    base(lane_, partition_, beat_, accent_, wait_, TYPE.REBOUND, typeMeta_, 1)
    {

    }

    public override Phrase clone()
    {
        return new ReboundPhrase(lane, partition, beat, accent, wait, (string[]) meta.Clone());
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        return Object.Instantiate(mp.notePrefab).GetComponent<Note>();
    }

    public override float getBlockFrame()
    {
        return MapSerializer.sing.noteBlockLen;
    }

    public override void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        // Doubles up (depending on if rebounds are spawned as pairs or rebounders)
        base.spawn(mp, spawnLane, spawnBeat, blockFrame);
        base.spawn(mp, spawnLane, spawnBeat+reboundDelta, blockFrame);
    }


}
