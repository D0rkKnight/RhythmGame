using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReboundPhrase : Phrase
{
    public float reboundBeatDist = 1.0f; // Distance of rebounded note (in beats)
    public int times = 1;

    public ReboundPhrase(int lane_, float beat_, int accent_, string[] typeMeta_, float priority_) :
    base(lane_, beat_, accent_, TYPE.REBOUND, typeMeta_, 2, priority_)
    {

    }

    // Core instantiator used by default spawner
    public override Note instantiateNote(MusicPlayer mp)
    {
        throw new System.Exception("Rebound note creation supposed to be post rasterization");

        /*ReboundNote note = (ReboundNote) instantiateNote(mp.reboundPrefab);
        note.reboundDelta = reboundBeatDist * mp.beatInterval;
        note.rebounds = times;

        return note;*/
    }

    public override void configNote(MusicPlayer mp, Note nObj, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        base.configNote(mp, nObj, spawnLane, spawnBeat, blockFrame, weight);
    }

    public override float getBlockFrame()
    {
        return MapSerializer.sing.noteBlockLen;
    }

    public override List<Note> spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        // Backup
        if (!MapSerializer.sing.genType[(int)TYPE.REBOUND])
        {
            for (int i = 0; i < times + 1; i++)
            {
                base.spawn(mp, spawnLane, spawnBeat + i * reboundBeatDist, blockFrame, weight, 
                    (MusicPlayer mp) =>
                {
                    return instantiateNote(mp.notePrefab);
                });
            }
            return null;
        }

        // Generates a sequence of ghosts
        // Ghosts also double as representatives of the rebound's future position
        // So they can block and be blocked
        GhostNote prev = null;
        for (int i=0; i<times+1; i++)
        {
            float ghostBeat = spawnBeat + i * reboundBeatDist;

            base.spawn(mp, spawnLane, ghostBeat, blockFrame, weight,
                (MusicPlayer mp) =>
                {
                    GhostNote ghost = (GhostNote) instantiateNote(mp.ghostPrefab);

                    // Checks to see if prev was blocked by the curr note
                    if (prev != null && prev.inPlayer)
                    {
                        ghost.prev = prev;
                        prev.next = ghost;
                    }

                    prev = ghost; // Advance prev pointer

                    return ghost;
                });
        }

        return null;
    }

    public override List<FieldDataPair> getFieldData()
    {
        List<FieldDataPair> data = base.getFieldData();
        data.Add(new FieldDataPair(MetaInputField.TYPE.TEXT, "Delta"));
        data.Add(new FieldDataPair(MetaInputField.TYPE.TEXT, "Times"));

        return data;
    }

    public override void writeToMeta()
    {
        base.writeToMeta();

        meta[0] = "" + reboundBeatDist;
        meta[1] = "" + times;
    }

    public override void readFromMeta()
    {
        base.readFromMeta();

        float tryRes;
        bool succ = float.TryParse(meta[0], out tryRes);

        if (succ) reboundBeatDist = tryRes;


        int tryInt;
        succ = int.TryParse(meta[1], out tryInt);

        if (succ) times = tryInt;
    }
}
