[System.Serializable]
public class Phrase
{
    public string partition;
    public int lane;
    public int accent;
    public float beat;

    public TYPE type;
    public float dur; // beats persisted
    public float wait; // beats until next phrase

    public enum TYPE
    {
        NONE, NOTE, HOLD, SENTINEL
    }

    public Phrase()
    {
        lane = 0;
        beat = 0;
        type = TYPE.NOTE;
        dur = 1f;
    }

    public Phrase(int lane_, string partition_, float beat_, int accent_, float wait_, TYPE type_)
    {
        lane = lane_;
        partition = partition_;
        beat = beat_;
        accent = accent_;
        type = type_;
        wait = wait_;
    }

    public Phrase clone()
    {
        Phrase p = new Phrase(lane, partition, beat, accent, wait, type);
        p.dur = dur;

        return p;
    }
}
