using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Ziggity zoo da
public abstract class StreamPhrase : Phrase
{
    public float dur = 0f;
    public int width = 2;
    public float rate = 1.0f;
    public bool recurse = false;

    public StreamPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, TYPE type_, string[] _meta, int metaLen_) : 
        base(lane_, partition_, beat_, accent_, wait_, type_, _meta, metaLen_)
    {
    }

    protected override bool genTypeBlock(out string res, List<string> meta)
    {
        base.genTypeBlock(out res, meta);
        meta.Add("" + dur);
        meta.Add("" + width);
        meta.Add("" + rate);
        meta.Add(recurse ? "T" : "F");

        return false;
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

            zzLane = streamNextLane(zzLane, mp, spawnLane, spawnBeat, blockFrame);
        }

        // Deactivate reference phrase if relevant
        if (callRecurse) mp.phraseQueue[myInd + 1].active = false;
    }

    // Determine the next lane when streaming
    public abstract int streamNextLane(int currLane, MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame);
}
