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


    public string fname;

    // Start is called before the first frame update
    void Start()
    {
        MusicPlayer mPlay = GetComponent<MusicPlayer>();

        string fpath = Application.dataPath + "/Maps/" + fname;
        StreamReader reader = new StreamReader(fpath);
        string data = reader.ReadToEnd();

        string[] tokens = data.Split('\n');
        ParseState state = ParseState.HEADER;

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
        
    }

    private ParseState parseHEADER(string tok)
    {
        if (tok.Equals("streamstart")) return ParseState.STREAM;

        // Break up a selection of header tags
        
        return ParseState.ERR;
    }

    private ParseState parseSTREAM(string tok)
    {
        switch (tok)
        {
            default:
                return ParseState.ERR;
        }
    }
}
