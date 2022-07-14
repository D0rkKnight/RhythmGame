using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public abstract class Phrase
{
    public int lane;
    public int accent;
    public float beat; // Beat no. that the phrase spawns on.
                       // Used in music player and in map editor but is regenerated every read/write cycle

    public TYPE type;
    public float wait; // beats until next phrase

    public bool active = true; // Whether this phrase is to be rasterized, or just a reference phrase

    // Metadata
    protected string[] meta;
    public static List<TypeEntry> typeTable = new List<TypeEntry>();
    public struct TypeEntry
    {
        public char charCode;
        public TYPE type;

        public Func<int, float, int, float, string[], Phrase> con;

        public TypeEntry(char charCode_, TYPE type_, Func<int, float, int, float, string[], Phrase> con_)
        {
            charCode = charCode_;
            type = type_;
            con = con_;
        }
    }


    public enum TYPE
    {
        NONE, NOTE, HOLD, ZIGZAG, SCATTER, REBOUND, SENTINEL
    }

    public Phrase()
    {
        lane = 0;
        beat = 0;
        type = TYPE.NOTE;
    }

    public Phrase(int lane_, float beat_, int accent_, float wait_, TYPE type_, string[] meta_, int _metaLen)
    {
        lane = lane_;
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
        o += "Beat: " + beat + "\n";
        o += "Accent: " + accent + "\n";
        o += "Wait: " + wait + "\n";
        o += "Type: " + type + "\n";

        return o;
    }

    // --------------------------------------- EDIT BELOW FUNCTION WHEN ADDING A NEW NOTE TYPE
    public static void init()
    {
        // Build phrase lookup table
        typeTable.Add(new TypeEntry('\0', TYPE.NONE,
            (lane_, beat_, accent_, wait_, meta_) => {
                return new NonePhrase(beat_, wait_);
            }
            ));

        typeTable.Add(new TypeEntry('\0', TYPE.NOTE,
            (lane_, beat_, accent_, wait_, meta_) => {
                return new NotePhrase(lane_, beat_, accent_, wait_);
            }
            ));

        typeTable.Add(new TypeEntry('H', TYPE.HOLD,
            (lane_, beat_, accent_, wait_, meta_) => {
                return new HoldPhrase(lane_, beat_, accent_, wait_, meta_);
            }
            ));

        typeTable.Add(new TypeEntry('Z', TYPE.ZIGZAG,
            (lane_, beat_, accent_, wait_, meta_) => {
                return new ZigzagPhrase(lane_, beat_, accent_, wait_, meta_);
            }
            ));

        typeTable.Add(new TypeEntry('S', TYPE.SCATTER,
            (lane_, beat_, accent_, wait_, meta_) => {
                return new ScatterPhrase(lane_, beat_, accent_, wait_, meta_);
            }
            ));

        typeTable.Add(new TypeEntry('X', TYPE.REBOUND,
            (lane_, beat_, accent_, wait_, meta_) => {
                return new ReboundPhrase(lane_, beat_, accent_, wait_, meta_);
            }
            ));
    }

    // Generates a phrase object given a universal list of parameters
    public static Phrase staticCon(int lane_, float beat_, int accent_, float wait_, string[] typeMeta, TYPE type_)
    {
        Phrase p = null;
        
        foreach (TypeEntry entry in typeTable)
        {
            if (entry.type == type_)
            {
                p = entry.con(lane_, beat_, accent_, wait_, typeMeta);
                break;
            }
        }

        if (p == null)
            Debug.LogWarning("Illegal phrase type "+type_);

        return p;
    }

    // Returns the appropriate type for the given typecode
    public static TYPE codeToType(string code)
    {
        if (code.Length == 1)
        {
            char c = code[0];
            foreach (TypeEntry entry in typeTable)
                if (entry.charCode == c) return entry.type;
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

        if (type == TYPE.NONE) return o; // None phrases hold only rhythm data

        // Look for type in typecode table
        string typeStr = "";
        foreach (TypeEntry entry in typeTable)
            if (entry.type == type)
            {
                if (entry.charCode != '\0') typeStr = "" + entry.charCode;
                break;
            }

        o += typeStr;

        // Write to meta list just in case
        writeToMeta();

        // Type metadata
        if (meta.Length > 0)
        {
            o += '(';
            for (int i=0; i<meta.Length; i++)
            {
                o += meta[i];
                if (i < meta.Length - 1) o += ',';
            }
            o += ')';
        }

        if (type != TYPE.NONE) o += lane + 1;
        for (int i = 0; i < accent; i++)
            o += "~";

        return o;
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
            NotePhrase ghost = (NotePhrase) staticCon(lane, beat, accent, wait, null, TYPE.NOTE);
            ghost.rasterize(ms);

            Debug.Log("Phrase ghosted");

            return;
        }

        // Short circuit if none type
        if (mutType == TYPE.NONE) return;


        // Limit accents
        int mutAccent = Mathf.Min(accent, ms.accentLim);

        // Empty = no lane specifier, defaults to the left lane of the category
        int mutLane = lane;

        // Given lane weights, calculate target lane
        // This will always push the lane inwards

        NoteColumn[] columns = MusicPlayer.sing.columns;

        if (!columns[mutLane].StreamOn)
        {
            mutLane = MusicPlayer.sing.getReroute(mutLane);
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
        Debug.Log(blockFrame);

        if (!noteValid(mp, spawnLane, spawnBeat, blockFrame))
        {
            Debug.LogWarning("Illegal note spawn blocked at " + lane + ", " + beat);
            return;
        }

        if (!checkNoteCollisions(mp, spawnLane, spawnBeat, blockFrame))
        {
            Debug.LogWarning("Note spawn blocked at " + lane + ", " + beat);
            return;
        }

        // Spawn note
        Note nObj = instantiateNote(mp);
        configNote(mp, nObj, spawnLane, spawnBeat, blockFrame);

        mp.addNote(nObj);
    }
    public virtual bool noteValid(MusicPlayer mp, int lane, float beat, float blockDur)
    {
        NoteColumn col = mp.columns[lane];
        if (!col.StreamOn)
        {
            Debug.LogWarning("Lane " + lane + " does not accept notes");
            return false;
        }

        return true;
    }

    // Returns whether the given note is alive after checks
    public virtual bool checkNoteCollisions(MusicPlayer mp, int lane_, float beat_, float blockDur_)
    {
        // Check for note collisions
        NoteColumn col = mp.columns[lane_];

        List<Note> collisions = new List<Note>();
        foreach (Note n in mp.notes)
        {   
            // Check opposing note's tail and head
            if (n.beat+n.blockDur >= beat_ && n.beat <= beat_ + blockDur_ && col == n.lane)
            {
                collisions.Add(n);
            }

            if (n.beat == beat_ && n.lane == col)
            {
                collisions.Add(n);
            }
        }

        if (collisions.Count > 0)
        {
            Debug.LogWarning("Spawning a note in a blocked segment: beat "
                + beat_);

            bool alive = true;
            foreach(Note n in collisions)
            {
                if (!tryReplaceNote(beat_, n))
                {
                    alive = false;
                    break;
                }
            }

            // Now if still alive, clear out the MP and force the spawn
            if (alive)
            {
                foreach (Note n in collisions)
                {
                    mp.notes.Remove(n);
                    UnityEngine.Object.Destroy(n.gameObject);
                }
                return true;
            } 
            else
            {
                return false; // Not alive so don't instantiate the note
            }
        }

        return true;
    }

    public bool tryReplaceNote(float beat, Note n)
    {
        // Scoring:
        // Pick the note that lands on the larger beat subdivision (8th > 16th)
        // Also consider adjacency to other notes on other columns

        // Get fractional component
        float frac1 = beat - (float) Math.Truncate(beat);       // The note to be spawned
        float frac2 = n.beat - (float)Math.Truncate(n.beat);    // The note being checked against

        // Count the number of doublings required to get a whole number
        float margin = 0.0001f;

        int pow1 = 0;
        while (frac1 - Math.Truncate(frac1) > margin)
        {
            frac1 *= 2;
            pow1++;
        }

        int pow2 = 0;
        while (frac2 - Math.Truncate(frac2) > margin)
        {
            frac2 *= 2;
            pow2++;
        }

        if (pow1 >= pow2) return true; // Means the tbspawned note is on a larger or same sized subdivision
        return false;
    }


    public abstract Note instantiateNote(MusicPlayer mp);

    public virtual void configNote(MusicPlayer mp, Note nObj, int spawnLane, float spawnBeat, float blockFrame)
    {
        nObj.lane = mp.columns[spawnLane];
        nObj.beat = spawnBeat;

        float bTime = mp.beatInterval * spawnBeat;
        nObj.hitTime = bTime;
        nObj.blockDur = blockFrame;
    }

    public virtual float getBlockFrame()
    {
        return 0;
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
    public NonePhrase(float beat_, float wait_) : base(0, beat_, 0, wait_, TYPE.NONE, null, 0)
    {

    }

    public override Phrase clone()
    {
        return new NonePhrase(beat, wait);
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        Debug.LogError("Cannot instantiate none phrase");
        return null;
    }
}