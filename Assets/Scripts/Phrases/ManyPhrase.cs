using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManyPhrase : Phrase
{
    public string group = "NULL"; // which group to mirror

    public ManyPhrase(int lane_, float beat_, int accent_, string[] meta_, float priority_) : 
        base(lane_, beat_, accent_, TYPE.MANY, meta_, 1, priority_)
    {
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        throw new Exception("Cannot instantiate ManyPhrase");
    }

    public override List<Note> spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        if (group.Equals("NULL")) return null;
        if (group.Equals(ownerGroup.name))
        {
            Debug.LogWarning("Would cause an infinite loop");
            return null;
        }

        // Looks up phrase group and shifts to right beat, then instantiates
        PhraseGroup grp = null;
        foreach (PhraseGroup g in ownerMap.groups)
        {
            if (g.name.Equals(group))
            {
                grp = g;
                break;
            }
        }

        if (grp == null) return null;

        // Clone every phrase in the new group and rasterize
        foreach (Phrase p in grp.phrases)
        {
            Phrase pc = p.fullClone();
            pc.ownerGroup = ownerGroup;
            pc.ownerMap = ownerMap;

            pc.highlight = highlight;
            pc.opacity = opacity;

            pc.beat += beat; // Root their beat 0 at this phrase's beat
            pc.lane = (pc.lane + lane) % MusicPlayer.sing.columns.Length; // Cycle spilled phrases
            pc.rasterize(MapSerializer.sing);
        }

        return null; // Guess this is unsupported for now
    }

    public override void writeMetaFields(List<MetaInputField> fields)
    {
        base.writeMetaFields(fields);

        fields[0].Label = "Group";
    }
    public override List<FieldDataPair> getFieldData()
    {
        List<FieldDataPair> data = base.getFieldData();
        data.Add(new FieldDataPair(MetaInputField.TYPE.TEXT, "Group"));

        return data;
    }

    public override void writeToMeta()
    {
        base.writeToMeta();

        meta[0] = group;
    }

    public override void readFromMeta()
    {
        base.readFromMeta();

        group = meta[0]; // Write in data

        if (group.Length == 0)
            group = "NULL";
    }
}