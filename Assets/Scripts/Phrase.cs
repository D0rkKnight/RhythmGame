using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public abstract class Phrase
{
    public string partition;
    public int lane;
    public int accent;
    public float beat; // Beat no. that the phrase spawns on.
                       // Used in music player and in map editor but is regenerated every read/write cycle

    public TYPE type;
    public float dur; // beats persisted
    public float wait; // beats until next phrase

    // Zigzag values
    public int leftLane;
    public int rightLane;

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

    public abstract Phrase clone();

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

        string typeStr;
        List<string> meta = new List<string>();

        if (!genTypeBlock(out typeStr, meta)) return o;

        Debug.Log(this.GetType());

        o += typeStr;

        /*switch (type)
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
        }*/

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

    // Returns whether the serialization completes
    protected virtual bool genTypeBlock(out string res, List<string> meta)
    {
        res = "";
        return true;
    }

    // Generates a phrase object given a universal list of parameters
    public static Phrase staticCon(int lane_, string partition_, float beat_, int accent_, float wait_, float dur_, TYPE type_)
    {
        Phrase p = null;
        switch (type_)
        {
            case TYPE.NONE:
                p = new NonePhrase(beat_, wait_);
                break;
            case TYPE.NOTE:
                p = new NotePhrase(lane_, partition_, beat_, accent_, wait_);
                break;
            case TYPE.HOLD:
                p = new HoldPhrase(lane_, partition_, beat_, accent_, wait_, dur_);
                break;
            default:
                Debug.LogError("Illegal phrase type");
                break;
        }

        return p;
    }
}

// Fields are stored in parent class for serialization
public class NotePhrase : Phrase
{
    public NotePhrase(int lane_, string partition_, float beat_, int accent_, float wait_) : base(lane_, partition_, beat_, accent_, wait_, TYPE.NOTE)
    {

    }

    public override Phrase clone()
    {
        return new NotePhrase(lane, partition, beat, accent, wait);
    }
}

public class NonePhrase : Phrase
{
    public NonePhrase(float beat_, float wait_) : base(1, "L", beat_, 0, wait_, TYPE.NONE)
    {

    }

    public override Phrase clone()
    {
        return new NonePhrase(beat, wait);
    }

    protected override bool genTypeBlock(out string res, List<string> meta)
    {
        res = "";
        return false;
    }
}

public class HoldPhrase : Phrase
{
    public HoldPhrase(int lane_, string partition_, float beat_, int accent_, float wait_, float dur_) : base(lane_, partition_, beat_, accent_, wait_, TYPE.HOLD)
    {
        dur = dur_;
    }

    public override Phrase clone()
    {
        return new HoldPhrase(lane, partition, beat, accent, wait, dur);
    }
    protected override bool genTypeBlock(out string res, List<string> meta)
    {
        res = "H";
        meta.Add("" + dur);
        return true;
    }

}