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

    [SerializeField]
    private List<BeatRow> beatRows;

    public float rowAdvance = 1.1f;
    public float scroll = 0f;

    public static MapEditor sing;

    public Phrase activePhrase = new NotePhrase(0, 0, 0, 1);
    public Phrase nonePhrase = new NonePhrase(0, 1);

    public int ActiveLane
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
    public bool hotswapQueued = false;

    public GameObject metaFieldPrefab;
    public List<InputField> metaFields;

    public KeyCode copyKey = KeyCode.C;

    public GameObject addPhraseEle;
    public int addPhraseIndex = 0;

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        beatRows = new List<BeatRow>();
        phraseMarker = transform.Find("Canvas/PhraseMarker");

        // Create meta fields
        metaFields = new List<InputField>();
        Transform metaFieldAnchor = transform.Find("Canvas/MetaFieldAnchor");
        for (int i=0; i<4; i++)
        {
            Transform field = Instantiate(metaFieldPrefab, metaFieldAnchor).transform;
            field.position = metaFieldAnchor.position + Vector3.down * 1 * i;
            metaFields.Add(field.GetComponent<InputField>());
        }

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
        if (hotswapQueued)
        {
            hotswap();
            hotswapQueued = false;
        }

        // Check for undo input
        if (Input.GetKeyDown(KeyCode.Z) /*&& Input.GetKey(KeyCode.LeftControl)*/) undo();

        // Move add phrase button
        Vector2 mPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float mY = mPos.y;
        for (int i=0; i<beatRows.Count-1; i++)
        {
            float bY1 = beatRows[i].transform.position.y;
            float bY2 = beatRows[i+1].transform.position.y;

            if (mY < bY1 && mY > bY2)
            {
                addPhraseIndex = i;
                break;
            }
        }

        // snap add button to inbetween
        float y1 = beatRows[addPhraseIndex].transform.position.y;
        float y2 = beatRows[addPhraseIndex+1].transform.position.y;

        addPhraseEle.transform.position = new Vector3(addPhraseEle.transform.position.x,
            (y1 + y2) / 2, addPhraseEle.transform.position.z);


        // Write field data to phrase
        activePhrase.readMetaFields(metaFields);

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

    public void insertPhraseAtMarker()
    {
        BeatRow row = Instantiate(rowPrefab, rowOrigin, false).GetComponent<BeatRow>();

        // Reset row data
        beatRows.Insert(addPhraseIndex+1, row);

        for (int i=0; i<beatRows.Count; i++)
            beatRows[i].setData(i + 1);

        updateBeatRows();

        // Push onto undo stack
        sing.edited = true;
        sing.imageQueued = true;
    }

    public void removeBeatRow(BeatRow caller)
    {
        Destroy(caller.gameObject);
        beatRows.Remove(caller);

        lastActiveRow--;

        for (int i = 0; i < beatRows.Count; i++)
            beatRows[i].setData(i + 1);

        updateBeatRows();

        // Push onto undo stack
        sing.edited = true;
        sing.imageQueued = true;
    }

    public void updateMetaField()
    {
        // Flag metadata input fields
        activePhrase.writeMetaFields(metaFields);
    }
}
