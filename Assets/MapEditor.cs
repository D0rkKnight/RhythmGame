using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MapEditor : MonoBehaviour, Clickable
{
    public GameObject rowPrefab;
    public Transform rowGenAnchor;
    private List<BeatRow> beatRows;

    int visibleRows = 10;
    public float rowAdvance = 1.1f;
    public float scroll = 0f;

    public static MapEditor sing;

    public enum ELEMENT{
        NONE, NOTE, HOLD, SENTINEL
    }

    public ELEMENT activeEle = ELEMENT.NOTE;

    private string activePartition = "L";
    public string ActivePartition // Unity buttons can interface with delegates
    {
        get { return activePartition; }
        set { activePartition = value; }
    }
    private int activeCol = 1;
    public int ActiveCol
    {
        get { return activeCol; }
        set { activeCol = value; }
    }

    public float activeBeatDur = 1;
    public Text beatIndicator;
    public InputField songTitleField;
    public InputField audioFileField;
    public InputField BPMField;

    public bool songPlayQueued = false;

    // Start is called before the first frame update
    void Start()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        beatRows = new List<BeatRow>();
        Transform canv = transform.Find("Canvas");

        Vector3 cPos = rowGenAnchor.position;
        for (int i=0; i< visibleRows; i++)
        {
            BeatRow row = Instantiate(rowPrefab, canv, false).GetComponent<BeatRow>();
            row.transform.position = cPos + Vector3.up * scroll;
            cPos += Vector3.down * rowAdvance;

            row.setData(i);
            beatRows.Add(row);
        }
    }

    private void updateBeatRows()
    {
        for (int i=0; i< visibleRows; i++)
        {
            BeatRow r = beatRows[i];

            r.transform.position = rowGenAnchor.transform.position + 
                Vector3.down * (-scroll + i * rowAdvance);
        }
    }

    public void play()
    {
        export(null, "playTemp");
        MusicPlayer.sing.resetSongEnv();
        MapSerializer.sing.genMap("playTemp.txt");
    }

    public void export()
    {
        export(null, null);
    }
    public void export(string forceSongName, string forceFileName)
    {
        // Write to header
        string mapName = songTitleField.text.Trim();
        string track = audioFileField.text.Trim();
        int bpm = int.Parse(BPMField.text.Trim());
        if (forceSongName != null) track = forceSongName;
        if (forceFileName != null) mapName = forceFileName;

        if (mapName.Length == 0 || track.Length == 0)
        {
            Debug.LogError("Invalid map/track name");
            return;
        }

        List<string> data = new List<string>();

        data.Add("mapname: " + mapName);
        data.Add("track: " + track);
        data.Add("bpm: " + bpm);

        data.Add("\nstreamstart");

        foreach (BeatRow row in beatRows)
        {
            data.Add(row.serialize());
        }


        string path = Application.streamingAssetsPath + "/Maps/" + mapName + ".txt";
        StreamWriter writer = new StreamWriter(path);
        
        foreach (string s in data) writer.WriteLine(s);
        writer.Close();
    }

    private void updateVisuals()
    {
        beatIndicator.text = activeBeatDur.ToString();
    }

    public void beatDurPow(int pow)
    {
        float tmp = activeBeatDur;

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
        if (beatInBounds(tmp)) activeBeatDur = tmp;

        updateVisuals();
    }

    public void beatDurAdd(float amt)
    {
        float tmp = activeBeatDur + amt;
        if (beatInBounds(tmp)) activeBeatDur = tmp;

        updateVisuals();
    }

    private bool beatInBounds(float amt)
    {
        if (amt > 0.0001 && amt < 10000) return true;
        return false;
    }

    public void resetBeatDur() { activeBeatDur = 1; updateVisuals(); }
    int Clickable.onClick(int code)
    {
        return 1;
    }

    int Clickable.onOver()
    {
        scroll -= Input.mouseScrollDelta.y;
        scroll = Mathf.Max(0, scroll);

        updateBeatRows();
        return 1;
    }
}
