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

    public bool active = true; // Whether this phrase is to be rasterized, or just a reference phrase

    public Map ownerMap = null;
    public PhraseGroup ownerGroup = null;

    public float priority;

    // Metadata
    protected string[] meta;
    public static List<TypeEntry> typeTable = new List<TypeEntry>();
    public struct TypeEntry
    {
        public char charCode;
        public TYPE type;

        public Func<int, float, int, string[], float, Phrase> con;

        public TypeEntry(char charCode_, TYPE type_, Func<int, float, int, string[], float, Phrase> con_)
        {
            charCode = charCode_;
            type = type_;
            con = con_;
        }
    }


    public enum TYPE
    {
        NONE, NOTE, HOLD, ZIGZAG, SCATTER, REBOUND, MANY, SENTINEL
    }

    public Phrase()
    {
        lane = 0;
        beat = 0;
        type = TYPE.NOTE;
    }

    public Phrase(int lane_, float beat_, int accent_, TYPE type_, string[] meta_, int _metaLen, float priority_)
    {
        lane = lane_;
        beat = beat_;
        accent = accent_;
        type = type_;
        meta = meta_;
        priority = priority_;

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
        o += "Type: " + type + "\n";

        return o;
    }

    // --------------------------------------- EDIT BELOW FUNCTION WHEN ADDING A NEW NOTE TYPE
    public static void init()
    {
        // Build phrase lookup table
        typeTable.Add(new TypeEntry('\0', TYPE.NONE,
            (lane_, beat_, accent_, meta_, priority_) => {
                return new NonePhrase(beat_);
            }
            ));

        typeTable.Add(new TypeEntry('\0', TYPE.NOTE,
            (lane_, beat_, accent_, meta_, priority_) => {
                return new NotePhrase(lane_, beat_, accent_, priority_);
            }
            ));

        typeTable.Add(new TypeEntry('H', TYPE.HOLD,
            (lane_, beat_, accent_, meta_, priority_) => {
                return new HoldPhrase(lane_, beat_, accent_, meta_, priority_);
            }
            ));

        typeTable.Add(new TypeEntry('Z', TYPE.ZIGZAG,
            (lane_, beat_, accent_, meta_, priority_) => {
                return new ZigzagPhrase(lane_, beat_, accent_, meta_, priority_);
            }
            ));

        typeTable.Add(new TypeEntry('S', TYPE.SCATTER,
            (lane_, beat_, accent_, meta_, priority_) => {
                return new ScatterPhrase(lane_, beat_, accent_, meta_, priority_);
            }
            ));

        typeTable.Add(new TypeEntry('X', TYPE.REBOUND,
            (lane_, beat_, accent_, meta_, priority_) => {
                return new ReboundPhrase(lane_, beat_, accent_, meta_, priority_);
            }
            ));

        typeTable.Add(new TypeEntry('M', TYPE.MANY,
            (lane_, beat_, accent_, meta_, priority_) => {
                return new ManyPhrase(lane_, beat_, accent_, meta_, priority_);
            }
            ));
    }

    // Generates a phrase object given a universal list of parameters
    public static Phrase staticCon(int lane_, float beat_, int accent_, string[] typeMeta, float priority_, TYPE type_)
    {
        Phrase p = null;
        
        foreach (TypeEntry entry in typeTable)
        {
            if (entry.type == type_)
            {
                p = entry.con(lane_, beat_, accent_, typeMeta, priority_);
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

        // Rhythm (use timestamp)
        o += "[" + beat + "]";

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

        o += "{" + priority + "}";

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

        if (ownerGroup == null || ownerMap == null)
            throw new Exception("Not in a rasterizable context");

        // Takes a phrase and spawns its notes
        // TODO: Don't use blocking frame, just check if it clashes with any notes in the buffer

        // Collapse types
        TYPE mutType = type;

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
        float weight = priority + accent; // Weight is based off of raw accent


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
                spawn(MusicPlayer.sing, sCol + j, beat, blockFrame, weight);
            }

        }
        else
        {
            spawn(MusicPlayer.sing, mutLane, beat, blockFrame, weight);
        }
    }

    // Generates this phrase attached to the given music player
    // Accepts generic arguments for mutability between phrase types
    public virtual List<Note> spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        return spawn(mp, spawnLane, spawnBeat, blockFrame, weight, instantiateNote);
    }

    // Spawn accepts an instantiator override
    public virtual List<Note> spawn(MusicPlayer mp, int spawnLane, float spawnBeat, float blockFrame, float weight, Func<MusicPlayer, Note> instantiator)
    {
        // TODO: Use this check for all higher complexity phrases
        if (!noteValid(mp, spawnLane, spawnBeat, blockFrame))
        {
            Debug.LogWarning("Illegal note spawn blocked at " + lane + ", " + beat);
            return null;
        }

        if (!checkNoteCollisions(mp, spawnLane, spawnBeat, blockFrame, weight))
        {
            Debug.LogWarning("Note spawn blocked at " + lane + ", " + beat);
            return null;
        }

        // Spawn note
        Note nObj = instantiator(mp);

        configNote(mp, nObj, spawnLane, spawnBeat, blockFrame, weight);
        mp.addNote(nObj);

        List<Note> o = new List<Note>();
        o.Add(nObj);
        return o;
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
    public virtual bool checkNoteCollisions(MusicPlayer mp, int lane_, float beat_, float blockDur_, float weight)
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
                if (!tryReplaceNote(beat_, weight, n))
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
                    n.blocked();

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

    // True means successful replace
    public bool tryReplaceNote(float beat, float weight, Note n)
    {
        // Scoring:
        // Pick the note that lands on the larger beat subdivision (8th > 16th)

        // Consider weight (based on accenting) first
        if (weight > n.weight) return true;
        if (weight < n.weight) return false;

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

    public virtual void configNote(MusicPlayer mp, Note nObj, int spawnLane, float spawnBeat, float blockFrame, float weight)
    {
        nObj.lane = mp.columns[spawnLane];
        nObj.beat = spawnBeat;
        nObj.weight = weight;

        float bTime = mp.beatInterval * spawnBeat;
        nObj.hitTime = bTime;
        nObj.blockDur = blockFrame;

        nObj.phrase = this; // Setup backlink

        nObj.resetInit(mp); // Also serves as an intializer
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

    public override bool Equals(System.Object obj)
    {
        if (!(obj is Phrase)) return false;

        Phrase p = (Phrase)obj;

        if (p.lane != lane || 
            p.beat != beat || 
            p.accent != accent || 
            p.type != type || 
            p.active != active ||
            p.priority != priority)
            return false;

        // Check meta fields
        if (meta.Length != p.meta.Length)
            return false;

        for (int i=0; i<meta.Length; i++)
        {
            if (!meta[i].Equals(p.meta[i]))
                return false;
        }

        return true;
    }
}

public class NonePhrase : Phrase
{
    public NonePhrase(float beat_) : base(0, beat_, 0, TYPE.NONE, null, 0, 0)
    {

    }

    public override Phrase clone()
    {
        return new NonePhrase(beat);
    }

    public override Note instantiateNote(MusicPlayer mp)
    {
        Debug.LogError("Cannot instantiate none phrase");
        return null;
    }
}