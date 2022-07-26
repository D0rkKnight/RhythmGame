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
        Color newCol = Color.white;

        if (phrase != null) {
            switch (phrase.type)
            {
                case Phrase.TYPE.NOTE:
                    newCol = Color.cyan;
                    break;
                case Phrase.TYPE.HOLD:
                    newCol = Color.magenta;
                    break;
                case Phrase.TYPE.NONE:
                    newCol = Color.white;
                    break;
                default:
                    newCol = Color.cyan;
                    Debug.LogWarning("Behavior not defined for note type: " + phrase.type);
                    break;
            }

            txt.text = serialize();
        }

        // Inherit opacity
        rend.color = new Color(newCol.r, newCol.g, newCol.b, rend.color.a);
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
        // Set active phrase as this
        if (MapEditor.sing.InteractMode == MapEditor.MODE.EDIT)
        {
            MapEditor.sing.setActivePhrase(phrase.clone());
            MapEditor.sing.selectedPhraseSlot = this;
        }

        return 0; // Catches input
    }
}
