using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ziggity zoo da
public class ZigzagPhrase : Phrase
{
    public ZigzagPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, float dur_) : base(lane_, partition_, beat_, accent_, wait_, TYPE.HOLD)
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
        return Object.Instantiate(mp.holdPrefab).GetComponent<Note>();
    }
}
