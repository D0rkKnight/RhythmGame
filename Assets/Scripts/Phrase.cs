using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Phrase
{
    public string partition;
    public int lane;
    public int accent;
    public float beat; // Beat no. that the phrase spawns on.
                       // Used in music player and in map editor but is regenerated every read/write cycle

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

    public override string ToString()
    {
        string o = "Phrase: ";
        o += "Lane: "+lane+"\n";
        o += "Partition: " + partition + "\n";
        o += "Beat: " + beat + "\n";
        o += "Accent: " + accent + "\n";
        o += "Wait: " + wait + "\n";
        o += "Duration: " + dur + "\n";
        o += "Type: " + type + "\n";

        return o;
    }

    // Metadata is force fed
    public string serialize()
    {
        string o = "";

        // Rhythm
        float wait_ = wait;
        if (wait_ > 0)
        {
            while (wait_ < 1 || wait_ - Mathf.Floor(wait_) != 0)
            {
                o = ">" + o; // Means the note is fast
                wait_ *= 2;
            }

            // Working with effectively integer value now
            while (wait_ > 1)
            {
                if (wait_ % 2 == 0)
                {
                    o = "<" + o;
                    wait_ /= 2;
                }
                else
                {
                    o = "|" + o;
                    wait_--;
                }
            }
        }

        List<string> meta = new List<string>();
        switch (type)
        {
            case Phrase.TYPE.NONE:
                return o; // Short circuit
            case Phrase.TYPE.NOTE:
                break;
            case Phrase.TYPE.HOLD:
                o += "H";
                meta.Add(""+dur);
                break;
            default:
                Debug.LogError("Behavior not defined for note type: " + type);
                break;
        }

        // Type metadata
        if (meta.Count > 0)
        {
            o += '(';
            for (int i=0; i<meta.Count; i++)
            {
                o += meta[i];
                if (i < meta.Count - 1) o += ',';
            }
            o += ')';
        }

        o += partition;
        o += lane;
        for (int i = 0; i < accent; i++)
            o += "~";

        return o;
    }
}
