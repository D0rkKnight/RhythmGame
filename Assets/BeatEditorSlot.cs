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
    public Phrase phrase = null;



    // Start is called before the first frame update
    void Awake()
    {
        rend = bg.GetComponent<SpriteRenderer>();
        parent = transform.parent.GetComponent<BeatRow>();
    }

    private void updateGraphics()
    {
        // No phrase attached
        if (phrase == null)
        {
            rend.color = Color.white;
            return;
        }

        switch (phrase.type)
        {
            case Phrase.TYPE.NOTE:
                rend.color = Color.cyan;
                break;
            case Phrase.TYPE.HOLD:
                rend.color = Color.magenta;
                break;
            case Phrase.TYPE.NONE:
                rend.color = Color.white;
                break;
            default:
                Debug.LogError("Behavior not defined for note type: " + phrase.type);
                break;
        }

        txt.text = serialize();
    }

    internal string serialize()
    {
        if (phrase == null) return ""; // Short circuit
        string o = "";

        // Rhythm
        float wait = phrase.wait;
        while (wait < 1 || wait - Mathf.Floor(wait) != 0)
        {
            o = ">"+o; // Means the note is fast
            wait *= 2;
        }

        // Working with effectively integer value now
        while (wait > 1)
        {
            if (wait % 2 == 0)
            {
                o = "<" + o;
                wait /= 2;
            } else
            {
                o = "|" + o;
                wait--;
            }
        }


        switch (phrase.type)
        {
            case Phrase.TYPE.NONE:
                return o; // Short circuit
            case Phrase.TYPE.NOTE:
                break;
            case Phrase.TYPE.HOLD:
                o += "H";
                break;
            default:
                Debug.LogError("Behavior not defined for note type: " + phrase.type);
                break;
        }

        o += phrase.partition;
        o += phrase.lane;
        for (int i=0; i<phrase.accent; i++)
            o += "~";

        return o;
    }

    public void setPhrase(Phrase p)
    {
        phrase = p;
        
        if (phrase != null && phrase.type != Phrase.TYPE.NONE && 
            parent.rowNum > MapEditor.sing.lastActiveRow)
            MapEditor.sing.lastActiveRow = parent.rowNum;

        updateGraphics();
    }

    int Clickable.onClick(int code)
    {
        switch (code) {
            case 0:
                setPhrase(MapEditor.sing.activePhrase.clone());
                break;
            case 1:
                Phrase p = MapEditor.sing.activePhrase.clone();
                p.type = Phrase.TYPE.NONE;
                setPhrase(p);
                break;
        }

        return 1;
    }

    int Clickable.onOver()
    {
        return 1;
    }
}
