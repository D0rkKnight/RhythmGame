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
        public struct Note
        {
            public int lane;
            public float beat;

            public Note(int lane_, float beat_)
            {
                this.lane = lane_;
                this.beat = beat_;
            }
        }

        public string name;
        public List<Note> notes;

        // Map is populated after creation
        public Map()
        {
            notes = new List<Note>();
        }

    }


    public string fname;
    private Map map;

    // Acceptable char pool for category data
    // L-left single R-right single
    List<char> catPool;
    List<char> lanePool;
    char accentChar = '~';

    float beat = 0;
    MusicPlayer mPlay;

    int lOff = 0;
    int rOff = 2;
    int width = 4;

    // Start is called before the first frame update
    void Start()
    {
        // Generate charpools
        catPool = new List<char>();
        lanePool = new List<char>();
        for (char c = 'a'; c <= 'z'; c++) catPool.Add(c);
        for (char c = 'A'; c <= 'Z'; c++) catPool.Add(c);
        for (char c = '0'; c <= '9'; c++) lanePool.Add(c);

        mPlay = GetComponent<MusicPlayer>();

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
        foreach (Map.Note n in map.notes) {
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


    private ParseState parseSTREAM(string tok)
    {
        // Single note reader
        // Read a column
        int i = 0;
        string col = "";
        while (i<tok.Length)
        {
            if (catPool.Contains(tok[i])) col += tok[i];
            else break;

            i++;
        }

        switch (col)
        {
            case "R":
            case "L":
                break;
            default:
                Debug.LogError("Illegal category: " + col);
                break;
        }

        // Read lane
        string lane = "";
        while (i<tok.Length)
        {
            if (lanePool.Contains(tok[i])) lane += tok[i];
            else break;

            i++;
        }

        // Read accent (can have different amounts)
        int accent = 0;
        while (i < tok.Length)
        {
            if (tok[i] == accentChar) accent++;
            else break;

            i++;
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

            for (int j=0; j<=accent; j++) map.notes.Add(new Map.Note(sCol + j, beat));

        } else {
            map.notes.Add(new Map.Note(l, beat));
        }

        // Normally look for a beat specifier, just advance beat here
        beat++;

        return ParseState.STREAM; // Persist state
    }
}
