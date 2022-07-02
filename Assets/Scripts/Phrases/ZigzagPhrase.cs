using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Ziggity zoo da
public class ZigzagPhrase : Phrase
{
    public float dur;
    public int width;
    public float rate = 1.0f;

    public ZigzagPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, float dur_, 
                        int width_, float rate_) : base(lane_, partition_, beat_, accent_, wait_, TYPE.HOLD)
    {
        dur = dur_;
        width = width_;
        rate = rate_;
    }

    public override Phrase clone()
    {
        return new ZigzagPhrase(lane, partition, beat, accent, wait, dur, width, rate);
    }
    protected override bool genTypeBlock(out string res, List<string> meta)
    {
        res = "Z";
        meta.Add("" + dur);
        meta.Add("" + width);
        meta.Add("" + rate);
        return true;
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        return Object.Instantiate(mp.notePrefab).GetComponent<Note>();
    }

    public override void writeMetaFields(List<InputField> fields)
    {
        base.writeMetaFields(fields);

        fields[0].gameObject.SetActive(true);
        fields[0].placeholder.GetComponent<Text>().text = "Duration";
        fields[0].text = "" + dur; // Write in data

        fields[1].gameObject.SetActive(true);
        fields[1].placeholder.GetComponent<Text>().text = "Width";
        fields[1].text = "" + width; // Write in data

        fields[2].gameObject.SetActive(true);
        fields[2].placeholder.GetComponent<Text>().text = "Rate";
        fields[2].text = "" + rate; // Write in data
    }

    public override void readMetaFields(List<InputField> fields)
    {
        base.readMetaFields(fields);

        float tryRes;
        bool succ = float.TryParse(fields[0].text, out tryRes);

        dur = 0;
        if (succ) dur = tryRes; // Write in data

        int tryInt;
        if (int.TryParse(fields[1].text, out tryInt)) width = tryInt;
        if (float.TryParse(fields[2].text, out tryRes)) rate = tryRes;
    }

    public override void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        int steps = (int) (dur * rate);
        int dir = Mathf.RoundToInt(Mathf.Sign(width));
        int bound = spawnLane + width - 1;

        int zzLane = spawnLane;
        float zzBeat = spawnBeat;

        Debug.Log("Calling ZZ spawner");

        // Zigzag pattern!
        for(int i=0; i<steps; i++)
        {
            zzBeat += dur / steps;

            base.spawn(mp, zzLane, zzBeat, blockFrame);

            // Shift lane
            zzLane += dir;
            if (zzLane == spawnLane || zzLane == bound) dir *= -1; // Bounce at edges
        }
    }
}
