using System.Collections.Generic;
using UnityEngine;
public partial class Map
{
    public string name;
    public string trackName;
    public int bpm;
    public float offset; // In beats

    public List<PhraseGroup> groups;

    // Map is populated after creation
    public Map()
    {
        groups = new List<PhraseGroup>();
    }

    public Map(string name_, string trackName_, int bpm_, float offset_, List<PhraseGroup> groups_)
    {
        name = name_;
        trackName = trackName_;
        bpm = bpm_;
        offset = offset_;

        // Dupe groups but not phrases
        // (since groups have little initialization overhead and phrase previz writes to the group)
        groups = new List<PhraseGroup>();
        foreach (PhraseGroup gp in groups_)
        {
            PhraseGroup ngp = new PhraseGroup(new List<Phrase>(), gp.name);
            foreach (Phrase p in gp.phrases)
            {
                ngp.phrases.Add(p);
                p.ownerMap = this; // Editor phrases will be linked to active map
            }

            groups.Add(ngp);
        }
    }

    public Map copy()
    {
        Map map = new Map(name, trackName, bpm, offset, new List<PhraseGroup>());

        // Go through groups and link them properly
        foreach (PhraseGroup gp in groups)
        {
            PhraseGroup ngp = new PhraseGroup(new List<Phrase>(), gp.name);

            foreach (Phrase p in gp.phrases)
            {
                Phrase np = p.fullClone();
                np.ownerGroup = ngp;
                np.ownerMap = this;

                ngp.phrases.Add(np);
            }

            map.groups.Add(ngp);
        }

        return map;
    }

    public void addPhraseToLastGroup(Phrase p)
    {
        // Add it regardless, checks are done later
        PhraseGroup grp = groups[groups.Count - 1];

        grp.phrases.Add(p);
        p.ownerMap = this;
        p.ownerGroup = grp;
    }
}
