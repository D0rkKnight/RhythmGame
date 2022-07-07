// Fields are stored in parent class for serialization
public class NotePhrase : Phrase
{
    public NotePhrase(int lane_, string partition_, float beat_, int accent_, float wait_) : 
        base(lane_, partition_, beat_, accent_, wait_, TYPE.NOTE, null, 0)
    {

    }

    public override Phrase clone()
    {
        return new NotePhrase(lane, partition, beat, accent, wait);
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