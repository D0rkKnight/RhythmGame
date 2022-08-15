// Fields are stored in parent class for serialization
public class NotePhrase : Phrase
{
    public NotePhrase(int lane_, float beat_, int accent_, float priority_) : 
        base(lane_, beat_, accent_, TYPE.NOTE, null, 0, priority_)
    {

    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        // Search note cache
        // (since this phrase only spawns 1 type of note any note it finds should be valid to config)
        if (nCache.Count > 0)
        {
            Note n = nCache[nCache.Count - 1];
            n.gameObject.SetActive(true);

            return n;
        }

        return UnityEngine.Object.Instantiate(mp.notePrefab);
    }

    public override float getBlockFrame()
    {
        return MapSerializer.sing.noteBlockLen;
    }
}