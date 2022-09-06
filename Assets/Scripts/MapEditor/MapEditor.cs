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

    public Phrase activePhrase = new NotePhrase(0, 0, 0, 1); // Should only be hard data

    public List<PhraseGroup> groups = new List<PhraseGroup>();
    public Workspace workspace = new Workspace(new List<BeatRow>(), null);

    public int ActiveLane
    {
        get { return activePhrase.lane; }
        set { activePhrase.lane = value; }
    }

    public TMP_InputField songTitleField;
    public TMP_InputField audioFileField;
    public TMP_InputField importField;
    public TMP_InputField authorField;

    public TMP_InputField BPMField;
    public TMP_InputField trackOffsetField;
    public TMP_InputField xTimeField;
    public float bpm;
    public float trackOffset;
    public int xTime;

    public TMP_Dropdown typeDropdown;
    private bool blockTypeUpdate = false;

    public TMP_Dropdown workspaceDropdown;
    public FloatLockedField beatInput;
    public FloatLockedField priorityInput;

    public TMP_Text codeInd;

    public bool songPlayQueued = false;

    Stack<Map> undoCache = new Stack<Map>(); // Includes the current state
    public bool edited = false;
    public bool imageQueued = false;
    public bool hotswapQueued = false;

    public MetaInputField metaFieldPrefab;
    public MetaInputField metaTogglePrefab;
    public List<MetaInputField> metaFields;

    public KeyCode copyKey = KeyCode.C;

    public GameObject addPhraseEle;
    public int addPhraseIndex = 0;

    public WorkspaceEditor workspaceEditor;

    public NoteColumn dragCol = null; // Null means not dragging
    public float dragBeatOffset = 0f; // Tracks where the drag selection begins within the phrase

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

            if (selectedPhraseSlot != null)
            {
                selectedPhraseSlot.deselect(); // Unselect whatever phrase
                queueHotswap(); // Needs to remove note highlight
            }
        }
    }
    public BeatEditorSlot selectedPhraseSlot = null;
    public bool draggingPhraseSlot = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;
        workspaceEditor = transform.Find("PhraseWorkspace").GetComponent<WorkspaceEditor>();

        metaFields = new List<MetaInputField>();

        // Create default workspace
        genDefWorkspaces();
    }

    private void Start()
    {
        List<string> data = exportString("tempname", audioFileField.text);
        Map image = MapSerializer.sing.parseTokens(data.ToArray());

        undoCache.Push(image);

        // Visual init
        activePhraseToEditorUI();

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
            if (blockTypeUpdate) return;

            System.Object newType;
            string typeName = typeDropdown.options[data].text;
            Enum.TryParse(typeof(Phrase.TYPE), typeName, out newType);

            Phrase p = sing.activePhrase;
            Phrase newPhrase = Phrase.staticCon(p.lane, p.beat, p.accent, null, p.priority, (Phrase.TYPE) newType);
            sing.writeActivePhrase(newPhrase);

            Debug.Log("type changed");
        });

        // Setup workspace dropdown
        workspaceDropdown.ClearOptions();
        options = new List<TMP_Dropdown.OptionData>();
        foreach (PhraseGroup space in groups)
        {
            options.Add(new TMP_Dropdown.OptionData(space.name));
        }

        workspaceDropdown.AddOptions(options);

        workspaceDropdown.onValueChanged.AddListener((data) =>
        {
            // Destroy existing rows
            foreach (BeatRow row in workspace.rows)
                Destroy(row.gameObject);
            workspace.rows.Clear();

            // Load in active rows
            foreach (Phrase p in groups[data].phrases)
                workspaceEditor.addPhraseEntry(p.hardClone());

            workspace.group = groups[data];
        });

        // Setup field cbs
        beatInput.cb = (float parse) =>
        {
            Phrase newPhrase = activePhrase.hardClone();
            newPhrase.beat = parse;

            writeActivePhrase(newPhrase);
        };

        priorityInput.cb = (float parse) =>
        {
            Phrase newPhrase = activePhrase.hardClone();
            newPhrase.priority = parse;

            writeActivePhrase(newPhrase);
        };
    }

    private void Update()
    {
        // Read field data
        // Needs to go first vs frame 1 hotswap
        if (float.TryParse(BPMField.text, out float tryBpm) && tryBpm > 0
            && tryBpm != bpm)
        {
            bpm = tryBpm;
            queueHotswap();
            queueImage();
        }
        if (float.TryParse(trackOffsetField.text, out float tryTrackOffset)
            && tryTrackOffset != trackOffset)
        {
            trackOffset = tryTrackOffset;
            queueHotswap();
            queueImage();
        }
        if (int.TryParse(xTimeField.text, out int tryXTime)
            && tryXTime != xTime)
        {
            xTime = tryXTime;
            queueHotswap();
            queueImage();
        }

        // Display active phrase
        if (activePhrase != null) 
            codeInd.text = activePhrase.serialize();

        // Process edits
        Map image = null;
        if (edited)
        {
            image = onEdit();
            edited = false;

            MusicPlayer.sing.pause();
        }
        if (imageQueued)
        {
            if (image == null)
                throw new Exception("Bad image request");

            undoCache.Push(image);
            imageQueued = false;
        }

        // Check for undo input
        if (Input.GetKeyDown(KeyCode.Z) /*&& Input.GetKey(KeyCode.LeftControl)*/) undo();

        // Write field data and timestamp to phrase
        // Change in meta field should queue hotswap
        activePhrase.readMetaFields(metaFields);

        // Sketchy and probably laggy
        if (InteractMode == MODE.EDIT)
        {
            if (selectedPhraseSlot != null)
            {
                bool requeue = selectedPhraseSlot.phrase.beat != activePhrase.beat;

                selectedPhraseSlot.setPhrase(activePhrase.hardClone(), true);

                if (requeue)
                    selectedPhraseSlot.requeuePhrase();
            }
        }

        // Mouse up (deselector)
        if (Input.GetMouseButtonUp(0))
        {
            draggingPhraseSlot = false;
            dragCol = null;
        }
    }

    public void play()
    {
        export(null, "playTemp");

        // Safe because the song environment gets reset
        MusicPlayer.sing.state = MusicPlayer.STATE.RUN;
        MusicPlayer.sing.clearSongEnv();
        MapSerializer.sing.playMap("playTemp.txt");
    }

    // Update visuals after an edit in the editor
    public Map onEdit()
    {
        Map image = hotswap(interactMode == MODE.WRITE);
        return image;
    }

    // Returns map that is hotswapped in
    public Map hotswap(bool loadActivePhrase = false)
    {
        Debug.Log("Hotswap commencing");

        // Pipe the data directly
        Map map = new Map(songTitleField.text+"_hotswap", audioFileField.text, authorField.text, (int) bpm, trackOffset, xTime, groups);

        // Tack on hovered item
        if (loadActivePhrase)
            foreach (PhraseGroup gp in map.groups)
                if (gp.name.Equals(workspace.group.name)) {
                    activePhrase.opacity = 0.2f;
                    activePhrase.ownerGroup = gp;
                    activePhrase.ownerMap = map;

                    gp.phrases.Add(activePhrase);

                    break;
                }

        // Just requeue the whole map while retaining track position
        MapSerializer.sing.loadMap(map, false);

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
        data.Add("author: " + authorField.text);
        data.Add("bpm: " + bpm);
        data.Add("offset: " + trackOffset);
        data.Add("xtime: " + xTime); // Use 0 as default for now

        data.Add("\nstreamstart");

        foreach (PhraseGroup group in groups)
        {
            data.Add(group.name);
            foreach (Phrase p in group.phrases)
            {
                data.Add(p.serialize());
            }

            data.Add("End\n");
        }

        return data;
    }

    public void clear()
    {
        foreach (BeatRow row in workspace.rows)
        {
            Destroy(row.gameObject);
        }

        groups.Clear();
        workspace.rows.Clear();
        workspace.group = null;
    }

    public void genDefWorkspaces()
    {
        clear();

        groups.Add(new PhraseGroup(new List<Phrase>(), "Main"));
        groups.Add(new PhraseGroup(new List<Phrase>(), "Sub1"));
        groups.Add(new PhraseGroup(new List<Phrase>(), "Sub2"));
        workspace.group = groups[0];
    }

    public void import(Map map)
    {
        clear(); // Clean slate

        foreach (PhraseGroup grp in map.groups)
        {
            groups.Add(grp.clone());
        }
        workspace.group = groups[0];

        // Import to phrase entries
        foreach (Phrase p in groups[0].phrases)
            workspaceEditor.addPhraseEntry(p.hardClone());

        songTitleField.text = map.name;
        audioFileField.text = map.trackName;
        BPMField.text = map.bpm.ToString();
        trackOffsetField.text = map.offset.ToString();
        authorField.text = map.author;
        xTimeField.text = map.xtime.ToString();

        edited = true;
    }

    public void import()
    {
        // This is what the button hooks into
        string fname = importField.text;
        if (!File.Exists(Path.Combine(Application.streamingAssetsPath, "Maps", fname)))
            return;

        Map map = MapSerializer.sing.parseMap(fname);

        import(map);
    }

    public void undo()
    {
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
        workspace.rows.Remove(entry);
        Destroy(entry.gameObject);

        queueHotswap();
    }

    public void addAccent(int amt)
    {
        activePhrase.accent = Mathf.Max(0, activePhrase.accent + amt);
    }

    public void writeActivePhrase(Phrase p)
    {
        if (!activePhrase.hardEquals(p))
        {
            activePhrase = p;
            activePhraseToEditorUI();
        }
    }

    public void queueHotswap()
    {
        // Field has been edited
        edited = true;
    }

    public void queueImage()
    {
        imageQueued = true;
    }

    public void activePhraseToEditorUI()
    {
        // Flag metadata input fields
        // activePhrase.writeMetaFields(metaFields);
        List<Phrase.FieldDataPair> fdata = activePhrase.getFieldData();

        // Clear old fields and construct new ones
        foreach (MetaInputField f in metaFields)
            Destroy(f.gameObject);
        metaFields.Clear();

        foreach (Phrase.FieldDataPair fd in fdata)
        {
            genMetaField(fd);
        }

        // Write data into said fields now that they are constructed
        activePhrase.writeMetaFields(metaFields);

        // Select right type dropdown option
        for (int i=0; i<typeDropdown.options.Count; i++)
        {
            TMP_Dropdown.OptionData option = typeDropdown.options[i];
            if (option.text.Equals(activePhrase.type.ToString()))
            {
                // Pick this option
                blockTypeUpdate = true; // Flag this bool to prevent the active phrase from being regenerated
                typeDropdown.value = i; // Will proc cb
                blockTypeUpdate = false;
            }
        }

        // Write to beat and priority input fields
        beatInput.setValue(activePhrase.beat);
        priorityInput.setValue(activePhrase.priority);
    }

    public void genMetaField(Phrase.FieldDataPair fd)
    {
        MetaInputField prefab;
        Transform metaFieldAnchor = transform.Find("Canvas/MetaFieldAnchor");
        switch (fd.type)
        {
            case MetaInputField.TYPE.TEXT:
                prefab = metaFieldPrefab;
                break;
            case MetaInputField.TYPE.TOGGLE:
                prefab = metaTogglePrefab;
                break;
            default:
                throw new Exception("Illegal Field Type");
        }

        // Create meta field
        MetaInputField field = Instantiate(prefab, metaFieldAnchor);
        field.transform.position = metaFieldAnchor.position + Vector3.down * 1 * metaFields.Count;
        metaFields.Add(field);

        field.Label = fd.label; // Copy over label
        field.fieldType = fd.dtype;
    }
}
