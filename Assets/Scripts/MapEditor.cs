using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MapEditor : MonoBehaviour, Clickable
{
    public GameObject rowPrefab;
    public Transform rowOrigin;
    public Transform rowLimit;
    public int lastActiveRow = 0;

    private Canvas canv;
    private List<BeatRow> beatRows;

    public float rowAdvance = 1.1f;
    public float scroll = 0f;

    public static MapEditor sing;

    public Phrase activePhrase = new Phrase(1, "L", 0, 0, 1, Phrase.TYPE.NOTE);
    public Phrase nonePhrase = new Phrase(1, "L", 0, 0, 1, Phrase.TYPE.NONE);
    public string ActivePartition // Unity buttons can interface with delegates
    {
        get { return activePhrase.partition; }
        set { activePhrase.partition = value; }
    }
    public int ActiveCol
    {
        get { return activePhrase.lane; }
        set { activePhrase.lane = value; }
    }

    public Text beatIndicator;
    public InputField songTitleField;
    public InputField audioFileField;
    public InputField BPMField;
    public InputField importField;
    public Text codeInd;
    public Transform phraseMarker;

    public bool songPlayQueued = false;

    Stack<Map> undoCache = new Stack<Map>(); // Includes the current state
    public bool edited = false;
    public bool imageQueued = false;

    public InputField metaField;

    public KeyCode copyKey = KeyCode.C;

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        beatRows = new List<BeatRow>();
        canv = transform.Find("Canvas").GetComponent<Canvas>();
        phraseMarker = transform.Find("Canvas/PhraseMarker");

        genRows();
    }

    private void Start()
    {
        List<string> data = exportString("tempname", audioFileField.text);
        Map image = MapSerializer.sing.parseTokens(data.ToArray());

        undoCache.Push(image);

        // Visual init
        updateMetaField();
    }

    private void Update()
    {
        // Display active phrase
        if (activePhrase != null) 
            codeInd.text = activePhrase.serialize();

        // Show where the song is at on the phrase list
        float currBeat = MusicPlayer.sing.getCurrBeat();
        int markerIndex = -1;
        for (int i=0; i<beatRows.Count-1; i++)
        {
            if (beatRows[i].slots[0].phrase.beat < currBeat &&
                beatRows[i+1].slots[0].phrase.beat > currBeat) {

                markerIndex = i;
                break;
            }
        }
        phraseMarker.position = rowOrigin.transform.position + 
            Vector3.down * (-scroll + rowAdvance * (markerIndex+0.5f)) + Vector3.left * 0.8f;

        // Process edits
        Map image = null;
        if (edited)
        {
            image = onEdit();
            edited = false;
        }
        if (imageQueued)
        {
            undoCache.Push(image);
            imageQueued = false;
        }

        // Check for undo input
        if (Input.GetKeyDown(KeyCode.Z) /*&& Input.GetKey(KeyCode.LeftControl)*/) undo();

        // Write field data to phrase
        switch (activePhrase.type)
        {
            case Phrase.TYPE.NONE:
                break;
            case Phrase.TYPE.NOTE:
                break;
            case Phrase.TYPE.HOLD:
                float tryRes;
                bool succ = float.TryParse(metaField.text, out tryRes);

                activePhrase.dur = 0;
                if (succ) activePhrase.dur = tryRes; // Write in data
                break;
            default:
                break;
        }

        genRows();
    }

    private void genRows()
    {
        genRows(-1);
    }
    private void genRows(int force)
    {
        while ((force > 0 && beatRows.Count < force) ||
            beatRows.Count == 0 || 
            beatRows[beatRows.Count - 1].transform.position.y > rowLimit.position.y)
        {
            BeatRow row = Instantiate(rowPrefab, rowOrigin, false).GetComponent<BeatRow>();
            row.setData(beatRows.Count+1);

            beatRows.Add(row);
            updateBeatRows();
        }
    }

    private void updateBeatRows()
    {
        for (int i=0; i< beatRows.Count; i++)
        {
            BeatRow r = beatRows[i];

            r.transform.position = rowOrigin.transform.position + 
                Vector3.down * (-scroll + i * rowAdvance);
        }
    }

    public void play()
    {
        export(null, "playTemp");

        // Safe because the song environment gets reset
        MusicPlayer.sing.state = MusicPlayer.STATE.RUN;
        MusicPlayer.sing.resetSongEnv();
        MapSerializer.sing.playMap("playTemp.txt");
    }

    public Map onEdit()
    {
        Debug.Log("edited");

        timestamp();
        Map image = hotswap();
        return image;
    }

    // Returns map that is hotswapped in
    public Map hotswap()
    {
        List<string> data = exportString(songTitleField.text+"_hotswap", audioFileField.text);
        MapSerializer mapSer = MapSerializer.sing;

        Map map = mapSer.parseTokens(data.ToArray());

        // Hotswap kinda depends (bpm change or track change will restage map)
        if (mapSer.activeMap == null || map.bpm != mapSer.activeMap.bpm || !map.trackName.Equals(mapSer.activeMap.trackName))
            MapSerializer.sing.stageMap(map);
        else
        {
            // delete all existing notes and then requeue new phrases
            MusicPlayer.sing.clearNotes();
            MusicPlayer.sing.clearPhraseQueue();

            foreach (Phrase p in map.phrases) MusicPlayer.sing.enqueuePhrase(p);
        }

        // Pause the mplayer
        MusicPlayer.sing.pause();

        // Rename map to have the right name
        map.name = songTitleField.text;
        return map;
    }

    public void export()
    {
        export(null, null);
    }
    public void export(string forceSongName, string forceFileName)
    {
        string mapName = songTitleField.text.Trim();
        string track = audioFileField.text.Trim();
        if (forceSongName != null) track = forceSongName;
        if (forceFileName != null) mapName = forceFileName;

        List<string> data = exportString(mapName, track);

        string path = Application.streamingAssetsPath + "/Maps/" + mapName + ".txt";
        StreamWriter writer = new StreamWriter(path);
        
        foreach (string s in data) writer.WriteLine(s);
        writer.Close();
    }

    private List<string> exportString(string mapName, string track)
    {
        // Write to header
        int bpm = int.Parse(BPMField.text.Trim());

        if (mapName.Length == 0 || track.Length == 0)
        {
            Debug.LogError("Invalid map/track name");
            return null;
        }

        List<string> data = new List<string>();

        data.Add("mapname: " + mapName);
        data.Add("track: " + track);
        data.Add("bpm: " + bpm);

        data.Add("\nstreamstart");

        for (int i = 0; i < lastActiveRow; i++)
        {
            data.Add(beatRows[i].serialize());
        }

        return data;
    }

    public void clear()
    {
        foreach (BeatRow br in beatRows)
        {
            Destroy(br.gameObject);
        }
        beatRows.Clear();
        lastActiveRow = 0;
    }

    public void import(Map map)
    {
        clear(); // Clean slate
        genRows(map.phrases.Count); // Get enough rows to contain every element

        // Phrases are linearly laid out
        for (int i = 0; i < map.phrases.Count; i++)
        {
            // Hackity hack
            beatRows[i].slots[0].setPhrase(map.phrases[i]);
        }

        songTitleField.text = map.name;
        audioFileField.text = map.trackName;
        BPMField.text = map.bpm.ToString();

        edited = true;
    }

    public void import()
    {
        string fname = importField.text;
        Map map = MapSerializer.sing.parseMap(fname);

        import(map);
    }

    public void undo()
    {
        Debug.Log("Undo");

        if (undoCache.Count > 1)
        {
            undoCache.Pop();
            Map lastImage = undoCache.Peek();
            import(lastImage);
        }

        // You cannot undo an undo.
        imageQueued = false;
    }

    private void updateVisuals()
    {
        beatIndicator.text = activePhrase.wait.ToString();
    }

    public void beatDurPow(int pow)
    {
        float tmp = activePhrase.wait;

        while (pow > 0)
        {
            tmp *= 2;
            pow--;
        }

        while (pow < 0)
        {
            tmp /= 2;
            pow++;
        }

        // Set some reasonable bounds to avoid rounding errors and overflow
        if (beatInBounds(tmp)) activePhrase.wait = tmp;

        updateVisuals();
    }

    public void beatDurAdd(float amt)
    {
        float tmp = activePhrase.wait + amt;
        if (beatInBounds(tmp)) activePhrase.wait = tmp;

        updateVisuals();
    }

    private bool beatInBounds(float amt)
    {
        if (amt > 0.0001 && amt < 10000) return true;
        return false;
    }

    public void resetBeatDur() { activePhrase.wait = 1; updateVisuals(); }
    int Clickable.onClick(int code)
    {
        return 1;
    }

    public void addAccent(int amt)
    {
        activePhrase.accent = Mathf.Max(0, activePhrase.accent + amt);
    }

    int Clickable.onOver()
    {
        scroll -= Input.mouseScrollDelta.y;
        scroll = Mathf.Max(0, scroll);

        updateBeatRows();
        return 1;
    }

    // TODO: Use this to timestamp serializer phrases too
    private void timestamp()
    {
        // Timestamp phrases (very simple algorithm)
        float currBeat = 0;
        for (int i = 0; i < beatRows.Count; i++)
        {
            Phrase p = beatRows[i].slots[0].phrase;
            p.beat = currBeat;

            // Advance beat
            currBeat += p.wait;
        }
    }

    // Metadata editing stuff
    private void activateField(string newName)
    {
        metaField.gameObject.SetActive(true);
        metaField.placeholder.GetComponent<Text>().text = newName;
    }

    private void deactivateField()
    {
        metaField.gameObject.SetActive(false);
    }

    public void updateMetaField()
    {
        Debug.Log("activating");

        // Flag metadata input fields
        switch (activePhrase.type)
        {
            case Phrase.TYPE.NONE:
                deactivateField();
                break;
            case Phrase.TYPE.NOTE:
                deactivateField();
                break;
            case Phrase.TYPE.HOLD:
                activateField("Hold Dur");
                metaField.text = "" + activePhrase.dur; // Write in data
                break;
            default:
                break;
        }
    }
}
