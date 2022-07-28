using System.Collections.Generic;
using UnityEngine;
public partial class Map
{
    public string name;
    public List<Phrase> notes;
    public List<Phrase> phrases;
    public float endBeat;
    public string trackName;
    public int bpm;
    public float offset; // In beats

    // Map is populated after creation
    public Map()
    {
        notes = new List<Phrase>();
        phrases = new List<Phrase>();
    }

    public void addPhrase(Phrase p)
    {
        // Add it regardless, checks are done later
        phrases.Add(p);
    }
}
