using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Ziggity zoo da
public class ZigzagPhrase : Phrase
{
    public float dur = 0f;
    public int width = 2;
    public float rate = 1.0f;
    public bool recurse = false;

    public ZigzagPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, string[] _meta) : 
        base(lane_, partition_, beat_, accent_, wait_, TYPE.HOLD, _meta, 4)
    {
    }

    public override Phrase clone()
    {
        return new ZigzagPhrase(lane, partition, beat, accent, wait, (string[]) meta.Clone());
    }
    protected override bool genTypeBlock(out string res, List<string> meta)
    {
        res = "Z";
        meta.Add("" + dur);
        meta.Add("" + width);
        meta.Add("" + rate);
        meta.Add(recurse ? "T" : "F");

        return true;
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        return Object.Instantiate(mp.notePrefab).GetComponent<Note>();
    }

    public override void writeMetaFields(List<InputField> fields)
    {
        base.writeMetaFields(fields);

        fields[0].placeholder.GetComponent<Text>().text = "Duration";
        fields[1].placeholder.GetComponent<Text>().text = "Width";
        fields[2].placeholder.GetComponent<Text>().text = "Rate";
        fields[3].placeholder.GetComponent<Text>().text = "Recurse";
    }

    public override void writeToMeta()
    {
        base.writeToMeta();

        meta[0] = "" + dur;
        meta[1] = "" + width;
        meta[2] = "" + rate;
        meta[3] = recurse ? "T" : "F"; // Write in data
    }

    public override void readFromMeta()
    {
        base.readFromMeta();

        float tryRes;
        bool succ = float.TryParse(meta[0], out tryRes);

        dur = 0;
        if (succ) dur = tryRes; // Write in data

        int tryInt;
        if (int.TryParse(meta[1], out tryInt)) width = tryInt;
        if (float.TryParse(meta[2], out tryRes)) rate = tryRes;

        recurse = false;
        if (meta[3].Trim().Equals("T")) recurse = true;
    }

    public override void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        int steps = (int) (dur * rate);
        int dir = Mathf.RoundToInt(Mathf.Sign(width));
        int bound = spawnLane + width - 1;

        int zzLane = spawnLane;
        float zzBeat = spawnBeat;

        // Clone next phrase somehow
        int myInd = mp.phraseQueue.IndexOf(this);
        bool callRecurse = recurse;

        if (myInd+1 >= mp.phraseQueue.Count) callRecurse = false; // Can't recure if there isn't a following phrase

        // Zigzag pattern!
        for(int i=0; i<steps; i++)
        {
            zzBeat += dur / steps;

            // Call the next phrase recursively
            if (callRecurse)
            {
                Phrase recursePhrase = mp.phraseQueue[myInd + 1].clone(); // Clone the next element
                recursePhrase.beat = zzBeat;

                // Try setting to the right column
                recursePhrase.lane = zzLane + 1;
                recursePhrase.partition = "L";

                recursePhrase.rasterize(MapSerializer.sing);
            }
            else // Spawn just notes otherwise
            {
                base.spawn(mp, zzLane, zzBeat, blockFrame);
            }

            // Shift lane
            zzLane += dir;
            if (zzLane == spawnLane || zzLane == bound) dir *= -1; // Bounce at edges
        }

        // Deactivate reference phrase if relevant
        if (callRecurse) mp.phraseQueue[myInd + 1].active = false;
    }
}
