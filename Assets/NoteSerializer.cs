using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[RequireComponent(typeof(MusicPlayer))]
public class NoteSerializer : MonoBehaviour
{
    private enum ParseState
    {
        HEADER, STREAM, ERR
    }

    public class Map
    {
        public class Note
        {
            public int lane;
            public float beat;

            public bool hold;
            public float holdLen;

            public Note()
            {
                lane = 0;
                beat = 0;
                hold = false;
                holdLen = 0;
            }

            public Note(int lane_, float beat_)
            {
                lane = lane_;
                beat = beat_;
                hold = false;
                holdLen = 0;
            }
            public Note(int lane_, float beat_, bool hold_, float holdLen_)
            {
                lane = lane_;
                beat = beat_;
                hold = hold_;
                holdLen = holdLen_;
            }

        }

        public string name;
        public List<Note> notes;

        // Map is populated after creation
        public Map()
        {
            notes = new List<Note>();
        }

        public void addNote(Note n)
        {
            MusicPlayer.Column col = MusicPlayer.sing.columns[n.lane];
            if (n.beat < col.blockedTil) Debug.LogError("Spawning a note in a blocked segment");
            else
            {
                notes.Add(n);
            }

            if (n.hold) {
                // Update column blocking
                col.blockedTil = Mathf.Max(col.blockedTil, n.beat + n.holdLen);
            }
        }

    }


    public string fnameOver;
    private Map map;

    // Acceptable char pool for category data
    // L-left single R-right single
    List<char> typePool;
    List<char> catPool;
    List<char> lanePool;
    List<char> accentPool;

    float beat = 0;
    MusicPlayer mPlay;

    int lOff = 0;
    int rOff = 2;
    int width = 4;

    // Start is called before the first frame update
    void Start()
    {
        // Generate charpools
        typePool = new List<char>();
        catPool = new List<char>();
        lanePool = new List<char>();
        accentPool = new List<char>();

        catPool.Add('L');
        catPool.Add('R');
        typePool.Add('H');
        for (char c = '0'; c <= '9'; c++) lanePool.Add(c);

        accentPool.Add('~');

        mPlay = GetComponent<MusicPlayer>();

        if (fnameOver.Length > 0) parseMap(fnameOver);
    }

    private void parseMap(string fname)
    {
        string fpath = Application.dataPath + "/Maps/" + fname;
        StreamReader reader = new StreamReader(fpath);
        string data = reader.ReadToEnd();

        string[] tokens = data.Split('\n');
        ParseState state = ParseState.HEADER;

        map = new Map();

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

        // Map is populated now, load into music player
        foreach (Map.Note n in map.notes)
        {
            mPlay.enqueueNote(n);
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
            switch (catName)
            {
                case "mapname":
                    map.name = tokSplit[1].Trim();
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
        StringScanner scanner = new StringScanner(tok);

        // Single note reader
        string type = scanner.getSegment(typePool);
        string col = scanner.getSegment(catPool);
        string lane = scanner.getSegment(lanePool);
        int accent = scanner.getSegment(accentPool).Length;



        // Write to note
        bool hold = false;
        float holdLen = 0;

        if (type.Equals("H"))
        {
            hold = true;
            holdLen = 2f;
        }

        // Empty = no lane specifier, defaults to the left lane of the category
        int l = 0;
        if (lane.Length > 0) l = int.Parse(lane)-1;

        // Given lane weights, calculate target lane
        switch (col)
        {
            case "R":
                l += rOff;
                break;
            case "L":
                l += lOff;
                break;
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

            for (int j=0; j<=accent; j++) map.addNote(new Map.Note(sCol + j, beat, hold, holdLen));

        } else {
            map.addNote(new Map.Note(l, beat, hold, holdLen));
        }

        // Normally look for a beat specifier, just advance beat here
        beat++;

        return ParseState.STREAM; // Persist state
    }
}
