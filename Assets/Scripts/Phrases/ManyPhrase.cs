using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManyPhrase : Phrase
{
    public string group = "NULL"; // which group to mirror

    public ManyPhrase(int lane_, float beat_, int accent_, string[] meta_) : 
        base(lane_, beat_, accent_, TYPE.MANY, meta_, 1)
    {
    }

    public override Phrase clone()
    {
        return new ManyPhrase(lane, beat, accent, (string[]) meta.Clone());
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        throw new Exception("Cannot instantiate ManyPhrase");
    }

    public override void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        if (group.Equals("NULL")) return;
        if (group.Equals(ownerGroup.name))
        {
            Debug.LogWarning("Would cause an infinite loop");
            return;
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

        if (grp == null) return;

        // Clone every phrase in the new group and rasterize
        foreach (Phrase p in grp.phrases)
        {
            Phrase pc = p.clone();
            pc.ownerGroup = ownerGroup;
            pc.ownerMap = ownerMap;

            pc.beat += beat; // Root their beat 0 at this phrase's beat
            pc.lane = (pc.lane + lane) % MusicPlayer.sing.columns.Length; // Cycle spilled phrases
            pc.rasterize(MapSerializer.sing);
        }
        
    }

    public override void writeMetaFields(List<InputField> fields)
    {
        base.writeMetaFields(fields);

        fields[0].placeholder.GetComponent<Text>().text = "Group";
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