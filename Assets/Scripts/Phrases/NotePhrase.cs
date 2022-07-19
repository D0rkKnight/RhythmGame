// Fields are stored in parent class for serialization
public class NotePhrase : Phrase
{
    public NotePhrase(int lane_, float beat_, int accent_) : 
        base(lane_, beat_, accent_, TYPE.NOTE, null, 0)
    {

    }

    public override Phrase clone()
    {
        return new NotePhrase(lane, beat, accent);
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        return UnityEngine.Object.Instantiate(mp.notePrefab).GetComponent<Note>();
    }

    public override float getBlockFrame()
    {
        return MapSerializer.sing.noteBlockLen;
    }
}