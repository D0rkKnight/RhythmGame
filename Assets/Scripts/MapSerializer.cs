using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[RequireComponent(typeof(MusicPlayer))]
public partial class MapSerializer : MonoBehaviour
{
    private enum ParseState
    {
        HEADER, STREAM, ERR
    }

    public string currMapFname;
    public Map activeMap;

    // Acceptable char pool for category data
    // L-left single R-right single
    List<char> typePool;
    List<char> catPool;
    List<char> lanePool;
    List<char> accentPool;
    List<char> beatPool;

    float readerBeat = 0;
    MusicPlayer mPlay;

    int lOff = 0;
    int rOff = 2;
    public int accentLim = 0;
    public bool[] genType = new bool[(int) Phrase.TYPE.SENTINEL];

    // Hacks for now
    int lDef = 1;
    int rDef = 2;

    int width = 4;

    public static MapSerializer sing;
    public bool loadQueued = false;

    // Start is called before the first frame update
    void Start()
    {
        // Setup singleton
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        // Generate charpools
        typePool = new List<char>();
        catPool = new List<char>();
        lanePool = new List<char>();
        accentPool = new List<char>();
        beatPool = new List<char>();

        catPool.Add('L');
        catPool.Add('R');
        typePool.Add('H');
        for (char c = '0'; c <= '9'; c++) lanePool.Add(c);

        accentPool.Add('~');
        beatPool.Add('|');
        beatPool.Add('<');
        beatPool.Add('>');

        mPlay = GetComponent<MusicPlayer>();
        genType[(int) Phrase.TYPE.NOTE] = true;
    }

    public void playMap()
    {
        playMap(currMapFname);
    }
    // Called when new map needs to be loaded into the music player
    public void playMap(string fname)
    {
        currMapFname = fname;

        if (fname.Length > 0)
        {
            stageMap(parseMap(fname));
        }

        // Don't do anything if we don't have a map to generate
    }

    public void stageMap(Map map)
    {
        activeMap = map;

        // Start music
        TrackPlayer.sing.loadTrack(activeMap.trackName);
        loadQueued = true;
    }

    public Map parseMap(string fname)
    {
        string fpath = Application.streamingAssetsPath + "/Maps/" + fname;
        StreamReader reader = new StreamReader(fpath);
        string data = reader.ReadToEnd();

        string[] tokens = data.Split('\n');
        return parseTokens(tokens);
    }

    public Map parseTokens(string[] tokens)
    {
        ParseState state = ParseState.HEADER;

        Map map = new Map();
        resetBeat();

        foreach (string tok in tokens)
        {
            // Clean the token
            string trimmed = tok.Trim();

            switch (state)
            {
                case ParseState.HEADER:
                    state = parseHEADER(trimmed, map);
                    break;
                case ParseState.STREAM:
                    state = parseSTREAM(trimmed, map);
                    break;
                case ParseState.ERR:
                    Debug.LogError("Filereader state machine error");
                    return null;
                default:
                    Debug.LogError("Filereader state machine out of bounds");
                    return null;

            }
        }

        return map;
    }

    public void Update()
    {
        if (loadQueued)
        {
            // Set music player bpm
            mPlay.BPM = activeMap.bpm;

            // Reset music player
            mPlay.resetSongEnv();

            // Map is populated now, load into music player
            foreach (Phrase p in activeMap.phrases)
            {
                mPlay.enqueuePhrase(p);
            }

            // Align music
            TrackPlayer.sing.resetTrack();
            loadQueued = false;
        }
    }

    private ParseState parseHEADER(string tok, Map map)
    {
        if (tok.Equals("streamstart")) return ParseState.STREAM;
        if (tok.Length == 0) return ParseState.HEADER;

        // Break up a selection of header tags
        string[] tokSplit = tok.Split(':');
        if (tokSplit.Length > 1)
        {
            // Grab the name
            string catName = tokSplit[0].Trim();

            // Process header name
            string value = tokSplit[1].Trim();
            switch (catName)
            {
                case "mapname":
                    map.name = value;
                    break;
                case "track":
                    map.trackName = value;
                    break;
                case "bpm":
                    map.bpm = int.Parse(value);
                    break;
                case "":
                    break;
                default:
                    Debug.LogError("Unrecognized map header token: " + catName);
                    break;
            }

            return ParseState.HEADER;
        } else
        {
            Debug.LogError("Poor delimiter for token with " + tokSplit.Length + " sections.");
        }

        return ParseState.ERR;
    }

    class StringScanner {
        public string str;
        public int ptr;

        public StringScanner(string str_)
        {
            this.str = str_;
            ptr = 0;
        }

        public string getSegment(List<char> charlist)
        {
            string o = "";
            while (ptr < str.Length)
            {
                if (charlist.Contains(str[ptr])) o += str[ptr];
                else break;

                ptr++;
            }

            return o;
        }
    }

    private ParseState parseSTREAM(string tok, Map map)
    {
        // Ignore empty tokens
        if (tok.Length == 0) return ParseState.STREAM;

        StringScanner scanner = new StringScanner(tok);

        // Single note reader
        string beatCode = scanner.getSegment(beatPool);
        string typeCode = scanner.getSegment(typePool);
        string part = scanner.getSegment(catPool);
        string lane = scanner.getSegment(lanePool);
        int accent = scanner.getSegment(accentPool).Length;

        // Write to note
        Phrase.TYPE type = Phrase.TYPE.NONE;
        float holdLen = 0;

        // If there is a partition, the type defaults to note
        if (part.Length > 0) type = Phrase.TYPE.NOTE;

        switch(typeCode)
        {
            case "H":
                type = Phrase.TYPE.HOLD;
                holdLen = 2;
                break;
        }

        float wait = getWait(beatCode);
        int l = 1;
        if (lane.Length > 0) l = int.Parse(lane);

        Phrase p = new Phrase(l, part, readerBeat, accent, wait, type)
        {
            dur = holdLen
        };

        map.addPhrase(p);


        advanceBeat(wait);

        return ParseState.STREAM; // Persist state
    }

    public void spawnNotes(Phrase p)
    {
        // Takes a phrase and spawns its notes
        // Don't use blocking frame, just check if it clashes with any notes in the buffer

        // Collapse types
        Phrase.TYPE type = p.type;
        if (!genType[(int) p.type]) type = Phrase.TYPE.NOTE;

        // Limit accents
        int accent = Mathf.Min(p.accent, accentLim);

        // Empty = no lane specifier, defaults to the left lane of the category
        int l = p.lane-1;

        // Given lane weights, calculate target lane
        int def = 0;
        bool hasElement = true; // Sometimes, we will get a beatcode but no element alongside it.
        switch (p.partition)
        {
            case "R":
                l += rOff;
                def = lDef;
                break;
            case "L":
                l += lOff;
                def = rDef;
                break;
            case "": // There's just no note here
                hasElement = false;
                break;
            default:
                Debug.LogError("Lane marker " + p.partition + " not recognized");
                break;
        }

        // If lane isn't available, default to default lanes
        // Accents for example will stack up and block each other
        MusicPlayer.Column[] columns = MusicPlayer.sing.columns;
        if (!columns[l].Active)
        {
            l = def;
        }

        // Double/triple up according to accents
        if (accent > 0)
        {

            // Count number of valid colums
            int validCols = 0;
            int leftValid = Mathf.Max(0, l - accent);
            for (int j = leftValid; j <= l; j++)
            {
                if (j + accent >= width) break;
                validCols++;
            }

            int sCol = Random.Range(leftValid, leftValid + validCols);

            for (int j = 0; j <= accent; j++) if (hasElement)
                {
                    // Spawn note directly
                    spawnIndNote(type, sCol + j, p.beat, p.dur);
                }

        }
        else
        {
            if (hasElement)
            {
                spawnIndNote(type, l, p.beat, p.dur);
            }
        }
    }

    // Has some overloads
    private void spawnIndNote(Phrase.TYPE type, int lane, float beat, float holdLen)
    {
        switch(type)
        {
            case Phrase.TYPE.NOTE:
                MusicPlayer.sing.spawnNote(lane, beat);
                break;
            case Phrase.TYPE.HOLD:
                MusicPlayer.sing.spawnHold(lane, beat, holdLen);
                break;
            default:
                Debug.LogError("Unrecognized phrase code: " + type);
                break;
        }
    }

    float getWait(string beatCode)
    {
        float b = 0;
        foreach (char c in beatCode)
        {
            switch (c)
            {
                case '|':
                    b++;
                    break;
                case '>':
                    b /= 2;
                    break;
                case '<':
                    b *= 2;
                    break;
                default:
                    Debug.LogError("Unrecognized beatcode: " + c);
                    break;
            }
        }

        return b;
    }

    void advanceBeat(float amt)
    {
        readerBeat += amt;
    }

    void resetBeat()
    {
        readerBeat = 0;
    }
}
