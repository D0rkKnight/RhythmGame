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

        phrase = new NonePhrase(0); // Random null phrase
        updateGraphics();

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
                rend.color = Color.cyan;
                Debug.LogWarning("Behavior not defined for note type: " + phrase.type);
                break;
        }

        txt.text = serialize();
    }

    internal string serialize()
    {
        if (phrase == null) return "";
        return phrase.serialize();
    }

    public void setPhrase(Phrase p)
    {
        phrase = p;

        updateGraphics();

        // Field has been edited
        MapEditor.sing.edited = true;
        MapEditor.sing.imageQueued = true;
    }

    int Clickable.onOver()
    {
        if (Input.GetKeyDown(MapEditor.sing.copyKey))
        {
            // Write phrase to active phrase
            MapEditor.sing.activePhrase = phrase.clone();
            MapEditor.sing.updateMetaField();
        }

        return 1;
    }

    public int onClick(int code)
    {
        return 0; // Catches input
    }
}
