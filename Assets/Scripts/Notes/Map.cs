using System.Collections.Generic;
using UnityEngine;
public partial class Map
{
    public string name;
    public float endBeat;
    public string trackName;
    public int bpm;
    public float offset; // In beats

    public List<PhraseGroup> groups;

    // Map is populated after creation
    public Map()
    {
        groups = new List<PhraseGroup>();
    }

    public void addPhraseToLastGroup(Phrase p)
    {
        // Add it regardless, checks are done later
        groups[groups.Count-1].phrases.Add(p);
    }
}
