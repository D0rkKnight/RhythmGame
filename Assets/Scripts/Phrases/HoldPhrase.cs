using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HoldPhrase : Phrase
{
    public float dur = 1.0f; // beats persisted

    public HoldPhrase(int lane_, float beat_, int accent_, string[] meta_) : 
        base(lane_, beat_, accent_, TYPE.HOLD, meta_, 1)
    {
    }

    public override Phrase clone()
    {
        return new HoldPhrase(lane, beat, accent, (string[]) meta.Clone());
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        if (!MapSerializer.sing.genType[(int)TYPE.HOLD])
            return UnityEngine.Object.Instantiate(mp.notePrefab).GetComponent<Note>();

        return UnityEngine.Object.Instantiate(mp.holdPrefab).GetComponent<Note>();
    }
    public override void configNote(MusicPlayer mp, Note nObj, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        base.configNote(mp, nObj, spawnLane, spawnBeat, blockFrame, weight);

        if (MapSerializer.sing.genType[(int)TYPE.HOLD])
        {
            // Set hold length
            HoldNote hn = (HoldNote)nObj;
            hn.holdBeats = dur;
            Transform bg = hn.bg;

            // Scale background bar appropriately
            bg.localScale = new Vector3(bg.localScale.x, mp.travelSpeed * mp.beatInterval * hn.holdBeats,
                bg.localScale.z);
        }
    }

    public override void writeMetaFields(List<InputField> fields)
    {
        base.writeMetaFields(fields);

        fields[0].placeholder.GetComponent<Text>().text = "Hold Dur";
    }

    public override void writeToMeta()
    {
        base.writeToMeta();

        meta[0] = "" + dur;
    }

    public override void readFromMeta()
    {
        base.readFromMeta();

        float tryRes;
        bool succ = float.TryParse(meta[0], out tryRes);

        if (succ) dur = tryRes; // Write in data
    }

    public override float getBlockFrame()
    {
        return Mathf.Max(dur, MapSerializer.sing.noteBlockLen);
    }
}