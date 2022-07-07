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
    public float wait; // beats until next phrase

    public bool active = true; // Whether this phrase is to be rasterized, or just a reference phrase

    // Metadata
    protected string[] meta;

    public static Dictionary<char, TYPE> typecodeMap = new Dictionary<char, TYPE>();


    public enum TYPE
    {
        NONE, NOTE, HOLD, ZIGZAG, SCATTER, SENTINEL
    }

    public Phrase()
    {
        lane = 0;
        beat = 0;
        type = TYPE.NOTE;
    }

    public Phrase(int lane_, string partition_, float beat_, int accent_, float wait_, TYPE type_, string[] meta_, int _metaLen)
    {
        lane = lane_;
        partition = partition_;
        beat = beat_;
        accent = accent_;
        type = type_;
        wait = wait_;
        meta = meta_;

        if (meta == null)
        {
            meta = new string[_metaLen]; // Void array
            writeToMeta(); // Write field defaults into meta cache
        }
        else
            readFromMeta(); // Read meta store into object fields
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
        o += "Type: " + type + "\n";

        return o;
    }



    // --------------------------------------- EDIT BELOW 2 FUNCTIONS WHEN ADDING A NEW NOTE TYPE
    public static void init()
    {
        // Build phrase lookup table
        typecodeMap.Add('H', TYPE.HOLD);
        typecodeMap.Add('Z', TYPE.ZIGZAG);
        typecodeMap.Add('S', TYPE.SCATTER);
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
                p = new HoldPhrase(lane_, partition_, beat_, accent_, wait_, typeMeta);
                break;
            case TYPE.ZIGZAG:
                p = new ZigzagPhrase(lane_, partition_, beat_, accent_, wait_, typeMeta);
                break;
            case TYPE.SCATTER:
                p = new ScatterPhrase(lane_, partition_, beat_, accent_, wait_, typeMeta);
                break;
            default:
                Debug.LogError("Illegal phrase type");
                break;
        }

        return p;
    }

    // Returns the appropriate type for the given typecode
    public static TYPE codeToType(string code)
    {
        if (code.Length == 1)
        {
            char c = code[0];
            if (typecodeMap.ContainsKey(c)) return typecodeMap[c];
        }
        return TYPE.SENTINEL; // Inconclusive
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
        if (!active)
        {
            Debug.LogWarning("Note rasterization blocked by inactivity");
            return;
        }

        // Takes a phrase and spawns its notes
        // TODO: Don't use blocking frame, just check if it clashes with any notes in the buffer

        // Collapse types
        TYPE mutType = type;
        if (!ms.genType[(int)type])
        {
            // Create a ghost phrase and rasterize that phrase instead
            NotePhrase ghost = (NotePhrase) staticCon(lane, partition, beat, accent, wait, null, TYPE.NOTE);
            ghost.rasterize(ms);

            Debug.Log("Phrase ghosted");

            return;
        }

        // Short circuit if none type
        if (mutType == TYPE.NONE) return;


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
        NoteColumn[] columns = MusicPlayer.sing.columns;

        if (!columns[mutLane].StreamOn)
        {
            mutLane = def;
        }

        // Get block frame
        float blockFrame = getBlockFrame();

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
                spawn(MusicPlayer.sing, sCol + j, beat, blockFrame);
            }

        }
        else
        {
            spawn(MusicPlayer.sing, mutLane, beat, blockFrame);
        }
    }

    // Generates this phrase attached to the given music player
    // Accepts generic arguments for mutability between phrase types
    public virtual void spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame)
    {
        if (!mp.noteValid(spawnLane, spawnBeat, blockFrame))
        {
            Debug.LogWarning("Note spawn blocked at " + lane + ", " + beat);
            return;
        }

        // Update column blocking
        NoteColumn col = mp.columns[spawnLane];
        if (blockFrame > 0)
        {
            // Update column blocking
            col.blockedTil = Mathf.Max(col.blockedTil, beat + blockFrame);
        }

        Note nObj = instantiateNote(mp);
        configNote(mp, nObj, spawnLane, spawnBeat, blockFrame);

        mp.addNote(nObj);
    }

    public abstract Note instantiateNote(MusicPlayer mp);

    public virtual void configNote(MusicPlayer mp, Note nObj, int spawnLane, float spawnBeat, float blockFrame)
    {
        nObj.lane = mp.columns[spawnLane];
        nObj.beat = spawnBeat;

        float bTime = mp.beatInterval * spawnBeat;
        nObj.hitTime = bTime;
    }

    public virtual float getBlockFrame()
    {
        return 0; // Doesn't advance blocking frame
    }

    // Writes meta contents to input field
    public virtual void writeMetaFields(List<InputField> fields)
    {
        writeToMeta(); // Reads object field values into meta cache

        // No fields
        foreach (InputField f in fields) f.gameObject.SetActive(false);

        for (int i=0; i<meta.Length; i++)
        {
            fields[i].gameObject.SetActive(true);
            fields[i].text = meta[i];
        }
    }


    // Read meta contents to phrase
    public void readMetaFields(List<InputField> fields)
    {
        for (int i = 0; i < meta.Length; i++)
            meta[i] = fields[i].text;

        readFromMeta(); // Writes newly read inputfield values into object fields
    }

    // Links meta store to real values
    public virtual void writeToMeta() { }
    public virtual void readFromMeta() { }
}

public class NonePhrase : Phrase
{
    public NonePhrase(float beat_, float wait_) : base(1, "L", beat_, 0, wait_, TYPE.NONE, null, 0)
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