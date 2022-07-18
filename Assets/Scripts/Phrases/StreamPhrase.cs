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

    public StreamPhrase(int lane_, float beat_, int accent_, float wait_, TYPE type_, string[] _meta, int metaLen_) : 
        base(lane_, beat_, accent_, wait_, type_, _meta, metaLen_)
    {
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

        // Calculate actual width and spawnlane given the situation
        // Shift bound first since the spawnLane ought to be valid
        int orgWLane = spawnLane + width - (int)Mathf.Sign(width);
        int wLane = orgWLane;
        wLane = Mathf.Clamp(wLane, 0, MapSerializer.sing.width-1);

        while (!mp.columns[wLane].StreamOn && mp.columns[wLane].defNoteReroute >= 0)
        {
            Debug.Log("Rerouted");

            // Figure out the column it's rerouting to
            wLane = mp.columns[wLane].defNoteReroute;
        }

        int shiftedBy = wLane - orgWLane;
        spawnLane += shiftedBy;

        // Shift the spawnLane to be valid too
        spawnLane = Mathf.Clamp(spawnLane, 0, MapSerializer.sing.width-1);
        while (!mp.columns[spawnLane].StreamOn && mp.columns[spawnLane].defNoteReroute >= 0)
        {
            // Figure out the column it's rerouting to
            spawnLane = mp.columns[spawnLane].defNoteReroute;
        }

        Debug.Log("Spawn: " + spawnLane + ", end: " + wLane + ", shifted by: " + shiftedBy);

        int zzLane = spawnLane;
        float zzBeat = spawnBeat;

        // Clone next phrase somehow
        int myInd = mp.phraseQueue.IndexOf(this);
        bool callRecurse = recurse;

        if (myInd+1 >= mp.phraseQueue.Count) callRecurse = false; // Can't recure if there isn't a following phrase

        // Stream notes
        for(int i=0; i<steps; i++)
        {
            zzBeat += dur / steps;

            // Call the next phrase recursively
            if (callRecurse)
            {
                Phrase recursePhrase = mp.phraseQueue[myInd + 1].clone(); // Clone the next element
                recursePhrase.beat = zzBeat;

                // Try setting to the right column
                recursePhrase.lane = zzLane;

                recursePhrase.rasterize(MapSerializer.sing);
            }
            else // Spawn just notes otherwise
            {
                base.spawn(mp, zzLane, zzBeat, blockFrame);
            }

            zzLane = streamNextLane(zzLane, mp, spawnLane, wLane, spawnBeat, blockFrame);
        }

        // Deactivate reference phrase if relevant
        if (callRecurse) mp.phraseQueue[myInd + 1].active = false;
    }

    // Determine the next lane when streaming
    public abstract int streamNextLane(int currLane, MusicPlayer mp, int spawnLane, int endLane, float spawnBeat, float blockFrame);

    public override float getBlockFrame()
    {
        return MapSerializer.sing.noteBlockLen;
    }
}
