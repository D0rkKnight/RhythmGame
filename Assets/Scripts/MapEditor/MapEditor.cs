using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class MapEditor : MonoBehaviour
{
    public static MapEditor sing;

    public Phrase activePhrase = new NotePhrase(0, 0, 0);
    public Phrase nonePhrase = new NonePhrase(0);
    public Workspace activeWorkspace = null;
    public List<Workspace> workspaces = new List<Workspace>();

    public int ActiveLane
    {
        get { return activePhrase.lane; }
        set { activePhrase.lane = value; }
    }

    public Text beatIndicator;
    public InputField songTitleField;
    public InputField audioFileField;
    public InputField importField;

    public InputField BPMField;
    public TMP_InputField trackOffsetField;
    public float bpm;
    public float trackOffset;

    public TMP_Dropdown typeDropdown;
    public TMP_Dropdown workspaceDropdown;
    public BeatField beatInput;

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

    public WorkspaceEditor workspaceEditor;

    public enum MODE
    {
        EDIT, WRITE
    }
    private MODE interactMode = MODE.WRITE;
    public MODE InteractMode
    {
        get
        {
            return interactMode;
        }
        set
        {
            interactMode = value;
            selectedPhraseSlot = null; // Unselect whatever phrase
        }
    }
    public BeatEditorSlot selectedPhraseSlot = null;
    public bool dragging = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        phraseMarker = transform.Find("Canvas/PhraseMarker");
        workspaceEditor = transform.Find("PhraseWorkspace").GetComponent<WorkspaceEditor>();

        // Create meta fields
        metaFields = new List<InputField>();
        Transform metaFieldAnchor = transform.Find("Canvas/MetaFieldAnchor");
        for (int i=0; i<4; i++)
        {
            Transform field = Instantiate(metaFieldPrefab, metaFieldAnchor).transform;
            field.position = metaFieldAnchor.position + Vector3.down * 1 * i;
            metaFields.Add(field.GetComponent<InputField>());
        }

        // Create default workspace
        genDefWorkspaces();
    }

    private void Start()
    {
        List<string> data = exportString("tempname", audioFileField.text);
        Map image = MapSerializer.sing.parseTokens(data.ToArray());

        undoCache.Push(image);

        // Visual init
        updateMetaField();

        // Assign types to type dropdown
        typeDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (Phrase.TypeEntry entry in Phrase.typeTable)
        {
            if (entry.type == Phrase.TYPE.NONE) continue;
            options.Add(new TMP_Dropdown.OptionData(entry.type.ToString()));
        }

        typeDropdown.AddOptions(options);

        // Submit event
        typeDropdown.onValueChanged.AddListener((data) =>
        {
            System.Object newType;
            string typeName = typeDropdown.options[data].text;
            Enum.TryParse(typeof(Phrase.TYPE), typeName, out newType);

            Phrase p = sing.activePhrase;
            Phrase newPhrase = Phrase.staticCon(p.lane, p.beat, p.accent, null, (Phrase.TYPE) newType);
            sing.setActivePhrase(newPhrase);
        });

        // Setup workspace dropdown
        workspaceDropdown.ClearOptions();
        options = new List<TMP_Dropdown.OptionData>();
        foreach (Workspace space in workspaces)
        {
            options.Add(new TMP_Dropdown.OptionData(space.name));
        }

        workspaceDropdown.AddOptions(options);

        workspaceDropdown.onValueChanged.AddListener((data) =>
        {
            // Deactivate existing rows
            foreach (BeatRow row in activeWorkspace.rows)
                row.gameObject.SetActive(false);

            activeWorkspace = workspaces[data];

            // Activate new rows
            foreach (BeatRow row in activeWorkspace.rows)
                row.gameObject.SetActive(true);

            Debug.Log("Activating workspace " + data);
        });
    }

    private void Update()
    {
        // Display active phrase
        if (activePhrase != null) 
            codeInd.text = activePhrase.serialize();

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

        // Write field data and timestamp to phrase
        activePhrase.readMetaFields(metaFields);

        // Sketchy and probably laggy
        if (InteractMode == MODE.EDIT)
        {
            if (selectedPhraseSlot != null)
            {
                selectedPhraseSlot.setPhrase(activePhrase.clone());
            }
        }

        // Read field data
        if (float.TryParse(BPMField.text, out float tryBpm) && tryBpm > 0
            && tryBpm != bpm)
        {
            bpm = tryBpm;
            markChange();
        }
        if (float.TryParse(trackOffsetField.text, out float tryTrackOffset)
            && tryTrackOffset != trackOffset)
        {
            trackOffset = tryTrackOffset;
            markChange();
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
        Map image = hotswap();
        return image;
    }

    // Returns map that is hotswapped in
    public Map hotswap()
    {
        Debug.Log("Hotswap commencing");

        List<string> data = exportString(songTitleField.text+"_hotswap", audioFileField.text);

        foreach (string s in data) Debug.Log(s);

        MapSerializer mapSer = MapSerializer.sing;
        Map map = mapSer.parseTokens(data.ToArray());

        // Just requeue the whole map while retaining track position
        MapSerializer.sing.stageMap(map, false);

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
        if (mapName.Length == 0 || track.Length == 0)
        {
            Debug.LogError("Invalid map/track name");
            return null;
        }

        List<string> data = new List<string>();

        data.Add("mapname: " + mapName);
        data.Add("track: " + track);
        data.Add("bpm: " + bpm);
        data.Add("offset: " + trackOffset);

        data.Add("\nstreamstart");

        // TODO
        foreach(BeatRow br in activeWorkspace.rows)
        {
            br.serialize(data);
        }

        return data;
    }

    public void clear()
    {
        foreach (Workspace space in workspaces)
        {
            foreach (BeatRow br in space.rows)
            {
                Destroy(br.gameObject);
            }
        }

        workspaces.Clear();
        genDefWorkspaces();
    }

    public void genDefWorkspaces()
    {
        workspaces.Add(new Workspace(new List<BeatRow>(), "Main"));
        workspaces.Add(new Workspace(new List<BeatRow>(), "Sub1"));
        workspaces.Add(new Workspace(new List<BeatRow>(), "Sub2"));

        activeWorkspace = workspaces[0];
    }

    public void import(Map map)
    {
        clear(); // Clean slate

        // Import to phrase entries
        for (int i = 0; i < map.phrases.Count; i++)
        {
            workspaceEditor.addPhraseEntry(map.phrases[i].clone());
        }

        songTitleField.text = map.name;
        audioFileField.text = map.trackName;
        BPMField.text = map.bpm.ToString();
        trackOffsetField.text = map.offset.ToString();

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

    public void removePhraseEntry(BeatRow entry)
    {
        activeWorkspace.rows.Remove(entry);
        Destroy(entry.gameObject);

        markChange();
    }

    public void addAccent(int amt)
    {
        activePhrase.accent = Mathf.Max(0, activePhrase.accent + amt);
    }

    public void updateMetaField()
    {
        // Flag metadata input fields
        activePhrase.writeMetaFields(metaFields);
    }

    public void setActivePhrase(Phrase p)
    {
        activePhrase = p;
        updateMetaField();
    }

    public void markChange()
    {
        // Field has been edited
        edited = true;
        imageQueued = true;
    }
}
