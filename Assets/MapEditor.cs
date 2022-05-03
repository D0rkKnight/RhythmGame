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

    public bool songPlayQueued = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        beatRows = new List<BeatRow>();
        canv = transform.Find("Canvas").GetComponent<Canvas>();

        Vector3 cPos = rowOrigin.position;
        Debug.Log(cPos);

        genRows();
    }

    private void Update()
    {
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

    public void hotswap()
    {
        List<string> data = exportString("tempname", audioFileField.text);
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

    public void import()
    {
        string fname = importField.text;
        Map map = MapSerializer.sing.parseMap(fname);

        genRows(map.phrases.Count); // Get enough rows to contain every element

        // Phrases are linearly laid out
        for (int i=0; i<map.phrases.Count; i++)
        {
            // Hackity hack
            beatRows[i].slots[0].setPhrase(map.phrases[i]);
        }

        songTitleField.text = map.name;
        audioFileField.text = map.trackName;
        BPMField.text = map.bpm.ToString();
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
}
