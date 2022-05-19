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

        o += typeStr;

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

    // Converts to notes
    public void rasterize(MapSerializer ms)
    {
        // Takes a phrase and spawns its notes
        // TODO: Don't use blocking frame, just check if it clashes with any notes in the buffer

        // Collapse types
        TYPE mutType = type;
        if (!ms.genType[(int)type]) mutType = Phrase.TYPE.NOTE;

        // Short circuit if none type
        if (mutType == Phrase.TYPE.NONE) return;

        // Limit accents
        int mutAccent = Mathf.Min(accent, ms.accentLim);

        // Empty = no lane specifier, defaults to the left lane of the category
        int mutLane = lane - 1;

        // Given lane weights, calculate target lane
        int def = 0;
        switch (partition)
        {
            case "R":
                mutLane += ms.rOff;
                def = ms.rDef;
                break;
            case "L":
                mutLane += ms.lOff;
                def = ms.lDef;
                break;
            default:
                Debug.LogError("Lane marker " + partition + " not recognized");
                break;
        }

        // If lane isn't available, default to default lanes
        // Accents for example will stack up and block each other
        MusicPlayer.Column[] columns = MusicPlayer.sing.columns;

        if (!columns[mutLane].Active)
        {
            mutLane = def;
        }

        // Double/triple up according to accents
        if (mutAccent > 0)
        {

            // Count number of valid colums
            int validCols = 0;
            int leftValid = Mathf.Max(0, mutLane - mutAccent);
            for (int j = leftValid; j <= mutLane; j++)
            {
                if (j + mutAccent >= ms.width) break;
                validCols++;
            }

            int sCol = UnityEngine.Random.Range(leftValid, leftValid + validCols);

            for (int j = 0; j <= mutAccent; j++)
            {
                // Spawn note directly
                spawn(MusicPlayer.sing, sCol + j, beat, dur, dur);
            }

        }
        else
        {
            spawn(MusicPlayer.sing, mutLane, beat, dur, dur);
        }
    }

    // Generates this phrase attached to the given music player
    // Accepts generic arguments for mutability between phrase types
    public virtual void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float duration)
    {
        if (!mp.noteValid(spawnLane, spawnBeat, blockFrame))
        {
            Debug.LogWarning("Note spawn blocked at " + lane + ", " + beat);
            return;
        }

        // Update column blocking
        MusicPlayer.Column col = mp.columns[spawnLane];
        if (blockFrame > 0)
        {
            // Update column blocking
            col.blockedTil = Mathf.Max(col.blockedTil, beat + blockFrame);
        }

        Note nObj = instantiateNote(mp);
        configNote(mp, nObj, spawnLane, spawnBeat, blockFrame, duration);

        mp.addNote(nObj);
    }

    public abstract Note instantiateNote(MusicPlayer mp);

    public virtual void configNote(MusicPlayer mp, Note nObj, int spawnLane, float spawnBeat, float blockFrame, float duration)
    {
        nObj.lane = mp.columns[spawnLane];
        nObj.beat = spawnBeat;

        float bTime = mp.beatInterval * spawnBeat;
        nObj.hitTime = bTime;
    }

    // Convert from typecode to type
    public static TYPE codeToType(string code)
    {
        switch (code)
        {
            case "H":
                return TYPE.HOLD;
        }

        return TYPE.SENTINEL; // Inconclusive
    }
    // Generates a phrase object given a universal list of parameters
    public static Phrase staticCon(int lane_, string partition_, float beat_, int accent_, float wait_, string[] typeMeta, TYPE type_)
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
                float dur_ = 0;

                if (typeMeta != null)
                {
                    dur_ = float.Parse(typeMeta[0]);
                }

                p = new HoldPhrase(lane_, partition_, beat_, accent_, wait_, dur_);
                break;
            default:
                Debug.LogError("Illegal phrase type");
                break;
        }

        return p;
    }

    // Writes meta contents to input field
    public virtual void writeMetaFields(List<InputField> fields)
    {
        // No fields
        foreach (InputField f in fields) f.gameObject.SetActive(false);
    }

    // Read meta contents to phrase
    public virtual void readMetaFields(List<InputField> fields)
    {
        // Don't do anything by default
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

    public override Note instantiateNote(MusicPlayer mp)
    {
        Debug.LogError("Cannot instantiate none phrase");
        return null;
    }
}