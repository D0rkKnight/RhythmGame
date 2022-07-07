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

    public int lOff = 0;
    public int rOff = 2;
    public int accentLim = 0;
    public bool[] genType = new bool[(int) Phrase.TYPE.SENTINEL];

    // Hacks for now
    public int lDef = 1;
    public int rDef = 2;
    public int width = 4;

    // # of beats that notes block for
    public float noteBlockLen = 0.25f;

    public static MapSerializer sing;
    public bool loadQueued = false;

    private void Awake()
    {
        // Setup singleton
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Generate charpools
        typePool = new List<char>();
        catPool = new List<char>();
        lanePool = new List<char>();
        accentPool = new List<char>();
        beatPool = new List<char>();

        // TODO: Read the typepool from Phrase's type-char listing
        catPool.Add('L');
        catPool.Add('R');

        foreach (char c in Phrase.typecodeMap.Keys)
            typePool.Add(c);

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
        string fpath = Path.Combine(Application.streamingAssetsPath, "Maps", fname);
        StreamReader reader = new StreamReader(fpath);
        string data = reader.ReadToEnd();

        string[] tokens = data.Split('\n');

        reader.Close();
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

    private ParseState parseSTREAM(string tok, Map map)
    {
        // Ignore empty tokens
        if (tok.Length == 0) return ParseState.STREAM;

        StringScanner scanner = new StringScanner(tok);

        // Single note reader
        string beatCode = scanner.getSegment(beatPool);
        string typeCode = scanner.getSegment(typePool);

        string[] typeMeta = null;
        if (typeCode.Length > 0) typeMeta = scanner.getMeta();

        string part = scanner.getSegment(catPool);
        string lane = scanner.getSegment(lanePool);
        int accent = scanner.getSegment(accentPool).Length;

        // Write to note
        Phrase.TYPE type = Phrase.TYPE.NONE;

        // If there is a partition, the type defaults to note
        if (part.Length > 0) type = Phrase.TYPE.NOTE;
        
        Phrase.TYPE codeOver = Phrase.codeToType(typeCode);
        if (codeOver != Phrase.TYPE.SENTINEL)
        {
            type = codeOver;
        }

        float wait = getWait(beatCode);
        int l = 1;
        if (lane.Length > 0) l = int.Parse(lane);

        Phrase p = Phrase.staticCon(l, part, readerBeat, accent, wait, typeMeta, type);

        map.addPhrase(p);
        advanceBeat(wait);

        return ParseState.STREAM; // Persist state
    }

    public void spawnNotes(Phrase p)
    {
        p.rasterize(this);
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
