using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

class TogglePhrase : Phrase
{
    public int node;
    public bool val;

    public TogglePhrase(float beat_, string[] meta_) : 
        base(0, beat_, 0, TYPE.TOGGLE, meta_, 2, 0)
    {
    }

    // Doesn't override the instantiator base,
    // but if you were feeding an instantiator you'd expect the normal spawn behavior
    public override List<Note> spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        // Skip checks since toggles can't really collide
        Note n = instantiateNote(mp);
        configNote(mp, n, spawnLane, spawnBeat, blockFrame, weight);

        return null;
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        return instantiateNote(mp.togglePrefab);
    }

    public override void configNote(MusicPlayer mp, Note nObj, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        base.configNote(mp, nObj, spawnLane, spawnBeat, blockFrame, weight);

        ToggleNote tog = (ToggleNote)nObj;

        tog.node = node;
        tog.val = val;
        tog.label.text = ((SkillTree.NODE)node).ToString() + " - " + (val ? "T" : "F");
    }

    public override List<FieldDataPair> getFieldData()
    {
        List<FieldDataPair> data = base.getFieldData();
        data.Add(new FieldDataPair(MetaInputField.TYPE.TEXT, "Skill name"));
        data.Add(new FieldDataPair(MetaInputField.TYPE.TOGGLE, "Val"));

        return data;
    }

    public override void writeToMeta()
    {
        base.writeToMeta();

        meta[0] = Enum.GetNames(typeof(SkillTree.NODE))[node];
        meta[1] = val ? "T" : "F";
    }

    public override void readFromMeta()
    {
        base.readFromMeta();

        foreach (SkillTree.NODE n in Enum.GetValues(typeof(SkillTree.NODE)))
            if (n.ToString().Equals(meta[0]))
            {
                node = (int) n;
                break;
            }

        if (meta[1].Trim().Equals("T")) val = true;
        if (meta[1].Trim().Equals("F")) val = false;
    }
}
