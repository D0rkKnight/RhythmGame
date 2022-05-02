
public class Note
{
    public int lane;
    public float beat;

    public bool hold;
    public float holdLen;

    public Note()
    {
        lane = 0;
        beat = 0;
        hold = false;
        holdLen = 0;
    }

    public Note(int lane_, float beat_)
    {
        lane = lane_;
        beat = beat_;
        hold = false;
        holdLen = 0;
    }
    public Note(int lane_, float beat_, bool hold_, float holdLen_)
    {
        lane = lane_;
        beat = beat_;
        hold = hold_;
        holdLen = holdLen_;
    }
}
