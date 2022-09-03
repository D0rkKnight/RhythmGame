using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Ziggity zoo da
public abstract class StreamPhrase : Phrase
{
    public int notes = 3;
    public int width = 2;
    public float noteLen = 1.0f;
    public bool recurse = false;

    public StreamPhrase(int lane_, float beat_, int accent_, TYPE type_, string[] _meta, int metaLen_, float priority_) : 
        base(lane_, beat_, accent_, type_, _meta, metaLen_, priority_)
    {
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        return instantiateNote(mp.notePrefab);
    }

    public override List<FieldDataPair> getFieldData()
    {
        List<FieldDataPair> data = base.getFieldData();
        data.Add(new FieldDataPair(MetaInputField.TYPE.TEXT, "Num notes"));
        data.Add(new FieldDataPair(MetaInputField.TYPE.TEXT, "Width"));
        data.Add(new FieldDataPair(MetaInputField.TYPE.TEXT, "Note len"));
        data.Add(new FieldDataPair(MetaInputField.TYPE.TOGGLE, "Recurse"));

        return data;
    }


    public override void writeToMeta()
    {
        base.writeToMeta();

        meta[0] = "" + notes;
        meta[1] = "" + width;
        meta[2] = "" + noteLen;
        meta[3] = recurse ? "T" : "F"; // Write in data
    }

    public override void readFromMeta()
    {
        base.readFromMeta();

        if (int.TryParse(meta[0], out int tryInt)) notes = tryInt;
        if (int.TryParse(meta[1], out tryInt)) width = tryInt;
        if (float.TryParse(meta[2], out float tryRes)) noteLen = tryRes;

        if (meta[3].Trim().Equals("T")) recurse = true;
    }

    public override List<Note> spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        // Calculate actual width and spawnlane given the situation
        // Shift bound first since the spawnLane ought to be valid
        int orgWLane = spawnLane + width - (int)Mathf.Sign(width);
        int wLane = orgWLane;
        wLane = Mathf.Clamp(wLane, 0, MapSerializer.sing.width-1);

        while (!mp.columns[wLane].StreamOn && mp.columns[wLane].defNoteReroute >= 0)
        {
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

        // Debug.Log("Spawn: " + spawnLane + ", end: " + wLane + ", shifted by: " + shiftedBy);

        int zzLane = spawnLane;
        float zzBeat = spawnBeat;

        // Clone next phrase somehow
        int myInd = ownerGroup.phrases.IndexOf(this);
        bool callRecurse = recurse;

        if (myInd+1 >= ownerGroup.phrases.Count) callRecurse = false; // Can't recure if there isn't a following phrase

        // Stream notes
        for(int i=0; i<notes; i++)
        {
            // Call the next phrase recursively
            if (callRecurse)
            {
                Phrase recursePhrase = ownerGroup.phrases[myInd + 1].fullClone(); // Clone the next element
                recursePhrase.beat = zzBeat;

                // Try setting to the right column
                recursePhrase.lane = zzLane;

                // Set this phrase's group as the new phrase's owner
                recursePhrase.ownerGroup = ownerGroup;
                recursePhrase.ownerMap = ownerMap;

                recursePhrase.rasterize(MapSerializer.sing);
            }
            else // Spawn just notes otherwise
            {
                base.spawn(mp, zzLane, zzBeat, blockFrame, weight);
            }

            zzLane = streamNextLane(zzLane, mp, spawnLane, wLane, spawnBeat, blockFrame);
            zzBeat += noteLen;
        }

        // Deactivate reference phrase if relevant
        if (callRecurse) ownerGroup.phrases[myInd + 1].active = false;

        return null; // Unsupported
    }

    // Determine the next lane when streaming
    public abstract int streamNextLane(int currLane, MusicPlayer mp, int spawnLane, int endLane, float spawnBeat, float blockFrame);

    public override float getBlockFrame()
    {
        return MapSerializer.sing.noteBlockLen;
    }
}
