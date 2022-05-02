using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeatEditorSlot : MonoBehaviour, Clickable
{
    public GameObject bg;
    public Text txt;
    private SpriteRenderer rend;

    public BeatRow parent;
    public MapEditor.ELEMENT noteEle = MapEditor.ELEMENT.NONE;
    public string partition;
    public int lane;
    public float beatDur = 1f;



    // Start is called before the first frame update
    void Start()
    {
        rend = bg.GetComponent<SpriteRenderer>();
    }

    private void updateGraphics()
    {
        switch (noteEle)
        {
            case MapEditor.ELEMENT.NONE:
                rend.color = Color.white;
                break;
            case MapEditor.ELEMENT.NOTE:
                rend.color = Color.cyan;
                break;
            case MapEditor.ELEMENT.HOLD:
                rend.color = Color.magenta;
                break;
            default:
                Debug.LogError("Behavior not defined for note type: " + noteEle);
                break;
        }

        txt.text = serialize();
    }

    internal string serialize()
    {
        if (noteEle == MapEditor.ELEMENT.NONE) return ""; // Short circuit
        string o = "";

        // Rhythm
        while (beatDur < 1 || beatDur - Mathf.Floor(beatDur) != 0)
        {
            o = ">"+o; // Means the note is fast
            beatDur *= 2;
        }

        // Working with effectively integer value now
        while (beatDur > 1)
        {
            if (beatDur % 2 == 0)
            {
                o = "<" + o;
                beatDur /= 2;
            } else
            {
                o = "|" + o;
                beatDur--;
            }
        }


        switch (noteEle)
        {
            case MapEditor.ELEMENT.NOTE:
                break;
            case MapEditor.ELEMENT.HOLD:
                o += "H";
                break;
            default:
                Debug.LogError("Behavior not defined for note type: " + noteEle);
                break;
        }

        o += partition;
        o += lane;

        return o;
    }

    int Clickable.onClick(int code)
    {
        switch (code) {
            case 0:
                noteEle = MapEditor.sing.activeEle;
                break;
            case 1:
                noteEle = MapEditor.ELEMENT.NONE;
                break;
        }

        MapEditor ed = MapEditor.sing;
        partition = ed.ActivePartition;
        lane = ed.ActiveCol;
        beatDur = ed.activeBeatDur;

        updateGraphics();
        return 1;
    }

    int Clickable.onOver()
    {
        return 1;
    }
}
