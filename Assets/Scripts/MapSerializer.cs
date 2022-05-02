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

    public partial class Map
    {
        public string name;
        public List<Note> notes;
        public float endBeat;
        public bool[] beatFrameOccupancy;
        public string trackName;
        public int bpm;

        // Map is populated after creation
        public Map(int width_)
        {
            notes = new List<Note>();
            beatFrameOccupancy = new bool[width_];
        }

        public void addNote(Note n)
        {
            MusicPlayer.Column col = MusicPlayer.sing.columns[n.lane];


            if (n.beat < col.blockedTil)
            {
                Debug.LogWarning("Spawning a note in a blocked segment: beat "
                    +n.beat+" when blocked til "+col.blockedTil);

                return;
            }

            if (beatFrameOccupancy[n.lane])
            {
                Debug.LogWarning("Lane " + n.lane + " occupied by another note on same frame");
                return;
            }

            if(!col.Active)
            {
                Debug.LogWarning("Lane " + n.lane + " deactivated");
                return;
            }

            notes.Add(n);

            if (n.hold) {
                // Update column blocking
                col.blockedTil = Mathf.Max(col.blockedTil, n.beat + n.holdLen);
            }
        }

    }


    public string currMapFname;
    private Map map;

    // Acceptable char pool for category data
    // L-left single R-right single
    List<char> typePool;
    List<char> catPool;
    List<char> lanePool;
    List<char> accentPool;
    List<char> beatPool;

    float beat = 0;
    MusicPlayer mPlay;

    int lOff = 0;
    int rOff = 2;
    public int accentLim = 0;
    public bool genHolds = false;

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
    }

    public void genMap()
    {
        genMap(currMapFname);
    }
    // Called when new map needs to be loaded into the music player
    public void genMap(string fname)
    {
        currMapFname = fname;

        resetBeat();
        if (fname.Length > 0) parseMap(fname);
        
        // Don't do anything if we don't have a map to generate
    }

    private void parseMap(string fname)
    {
        string fpath = Application.streamingAssetsPath + "/Maps/" + fname;
        StreamReader reader = new StreamReader(fpath);
        string data = reader.ReadToEnd();

        Debug.Log("Loading song at " + fpath);

        string[] tokens = data.Split('\n');
        ParseState state = ParseState.HEADER;

        map = new Map(width);

        foreach (string tok in tokens)
        {
            // Clean the token
            string trimmed = tok.Trim();

            switch (state)
            {
                case ParseState.HEADER:
                    state = parseHEADER(trimmed);
                    break;
                case ParseState.STREAM:
                    state = parseSTREAM(trimmed);
                    break;
                case ParseState.ERR:
                    Debug.LogError("Filereader state machine error");
                    return;
                default:
                    Debug.LogError("Filereader state machine out of bounds");
                    return;

            }
        }

        // Start music
        TrackPlayer.sing.loadTrack(map.trackName);
        loadQueued = true;
    }

    public void Update()
    {
        if (loadQueued)
        {
            Debug.Log("Loading queue");

            // Set music player bpm
            mPlay.BPM = map.bpm;

            // Map is populated now, load into music player
            foreach (Note n in map.notes)
            {
                mPlay.enqueueNote(n);
            }

            // Align music
            TrackPlayer.sing.resetTrack();
            loadQueued = false;
        }
    }

    private ParseState parseHEADER(string tok)
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
                    Debug.LogError("Unrecognized map header token: "+catName);
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

    private ParseState parseSTREAM(string tok)
    {
        // Ignore empty tokens
        if (tok.Length == 0) return ParseState.STREAM;

        StringScanner scanner = new StringScanner(tok);

        // Single note reader
        string beatCode = scanner.getSegment(beatPool);
        string type = scanner.getSegment(typePool);
        string col = scanner.getSegment(catPool);
        string lane = scanner.getSegment(lanePool);
        int accent = scanner.getSegment(accentPool).Length;



        // Write to note
        bool hold = false;
        float holdLen = 0;

        if (type.Equals("H") && genHolds)
        {
            hold = true;
            holdLen = 2f;
        }

        // Limit accents
        accent = Mathf.Min(accent, accentLim);

        // Empty = no lane specifier, defaults to the left lane of the category
        int l = 0;
        if (lane.Length > 0) l = int.Parse(lane)-1;

        // Given lane weights, calculate target lane
        int def = 0;
        bool hasElement = true; // Sometimes, we will get a beatcode but no element alongside it.
        switch (col)
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
                Debug.LogError("Lane marker " + col + " not recognized");
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
        if (accent > 0) {

            // Count number of valid colums
            int validCols = 0;
            int leftValid = Mathf.Max(0, l - accent);
            for (int j = leftValid; j <= l; j++)
            {
                if (j + accent >= width) break;
                validCols++;
            }

            int sCol = Random.Range(leftValid, leftValid + validCols);

            for (int j=0; j<=accent; j++) if (hasElement) 
                    map.addNote(new Note(sCol + j, beat, hold, holdLen));

        } else {
            if (hasElement) 
                map.addNote(new Note(l, beat, hold, holdLen));
        }

        advanceBeat(beatCode);

        return ParseState.STREAM; // Persist state
    }

    void advanceBeat(string beatCode)
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

        advanceBeat(b);
    }

    void advanceBeat(float amt)
    {
        beat += amt;
        
        if (map != null) for (int i=0; i<map.beatFrameOccupancy.Length; i++)
        {
            map.beatFrameOccupancy[i] = false;
        }
    }

    void resetBeat()
    {
        beat = 0;

        if (map != null) for (int i = 0; i < map.beatFrameOccupancy.Length; i++)
        {
            map.beatFrameOccupancy[i] = false;
        }
    }
}
