using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReboundPhrase : Phrase
{
    float reboundBeatDist = 1.0f; // Distance of rebounded note (in beats)
    int times = 1;

    public ReboundPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, string[] typeMeta_) :
    base(lane_, partition_, beat_, accent_, wait_, TYPE.REBOUND, typeMeta_, 2)
    {

    }

    public override Phrase clone()
    {
        return new ReboundPhrase(lane, partition, beat, accent, wait, (string[]) meta.Clone());
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        ReboundNote note = Object.Instantiate(mp.reboundPrefab).GetComponent<ReboundNote>();
        note.reboundDelta = reboundBeatDist * mp.beatInterval;
        note.rebounds = times;
        note.reboundHeight = 3.0f;

        return note;
    }

    public override float getBlockFrame()
    {
        return MapSerializer.sing.noteBlockLen;
    }

    public override void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        // Doubles up (depending on if rebounds are spawned as pairs or rebounders)
        /*for (int i=0; i<times; i++)
        {
            base.spawn(mp, spawnLane, spawnBeat + reboundDelta * i, blockFrame);
        }*/

        base.spawn(mp, spawnLane, spawnBeat, blockFrame);
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
