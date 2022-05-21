using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HoldPhrase : Phrase
{
    public float dur; // beats persisted

    public HoldPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, float dur_) : base(lane_, partition_, beat_, accent_, wait_, TYPE.HOLD)
    {
        dur = dur_;
    }

    public override Phrase clone()
    {
        return new HoldPhrase(lane, partition, beat, accent, wait, dur);
    }
    protected override bool genTypeBlock(out string res, List<string> meta)
    {
        res = "H";
        meta.Add("" + dur);
        return true;
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        return UnityEngine.Object.Instantiate(mp.holdPrefab).GetComponent<Note>();
    }
    public override void configNote(MusicPlayer mp, Note nObj, int spawnLane, float spawnBeat, float blockFrame)
    {
        base.configNote(mp, nObj, spawnLane, spawnBeat, blockFrame);

        Transform bg = nObj.transform.Find("HoldBar");

        // Set hold length
        HoldNote hn = (HoldNote)nObj;
        hn.holdBeats = dur;

        // Scale background bar appropriately
        bg.localScale = new Vector3(bg.localScale.x, mp.travelSpeed * mp.beatInterval * hn.holdBeats,
            bg.localScale.z);
    }

    public override void writeMetaFields(List<InputField> fields)
    {
        base.writeMetaFields(fields);

        fields[0].gameObject.SetActive(true);
        fields[0].placeholder.GetComponent<Text>().text = "Hold Dur";
        fields[0].text = "" + dur; // Write in data
    }

    public override void readMetaFields(List<InputField> fields)
    {
        base.readMetaFields(fields);

        float tryRes;
        bool succ = float.TryParse(fields[0].text, out tryRes);

        dur = 0;
        if (succ) dur = tryRes; // Write in data
    }
}