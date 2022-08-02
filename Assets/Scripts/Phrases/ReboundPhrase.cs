using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReboundPhrase : Phrase
{
    float reboundBeatDist = 1.0f; // Distance of rebounded note (in beats)
    int times = 1;

    public ReboundPhrase(int lane_, float beat_, int accent_, string[] typeMeta_) :
    base(lane_, beat_, accent_, TYPE.REBOUND, typeMeta_, 2)
    {

    }

    public override Phrase clone()
    {
        return new ReboundPhrase(lane, beat, accent, (string[]) meta.Clone());
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        ReboundNote note = Object.Instantiate(mp.reboundPrefab).GetComponent<ReboundNote>();
        note.reboundDelta = reboundBeatDist * mp.beatInterval;
        note.rebounds = times;

        return note;
    }

    public override float getBlockFrame()
    {
        return MapSerializer.sing.noteBlockLen;
    }

    public override List<Note> spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        // Doubles up (depending on if rebounds are spawned as pairs or rebounders)
        /*for (int i=0; i<times; i++)
        {
            base.spawn(mp, spawnLane, spawnBeat + reboundDelta * i, blockFrame);
        }*/

        List<Note> nList = base.spawn(mp, spawnLane, spawnBeat, blockFrame, weight);
        if (nList == null) return null; // Failed to spawn

        ReboundNote reboundN = (ReboundNote) nList[0];

        // Generates a sequence of ghosts
        // Ghosts also double as representatives of the rebound's future position
        // So they can block and be blocked
        for (int i=0; i<times; i++)
        {
            GhostNote ghost = Object.Instantiate(mp.ghostPrefab);
            ghost.parent = reboundN;

            float ghostBeat = spawnBeat + (i + 1) * reboundBeatDist;

            configNote(mp, ghost, spawnLane, ghostBeat, blockFrame, weight);
            mp.addNote(ghost);

            reboundN.ghosts.Add(ghost);
        }

        return null;
    }

    public override void writeMetaFields(List<InputField> fields)
    {
        base.writeMetaFields(fields);

        fields[0].placeholder.GetComponent<Text>().text = "Delta";
        fields[1].placeholder.GetComponent<Text>().text = "Times";
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
