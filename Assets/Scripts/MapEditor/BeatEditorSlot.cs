using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeatEditorSlot : MonoBehaviour, Clickable
{
    public GameObject bg;
    public TMPro.TMP_Text txt;
    private SpriteRenderer rend;

    public BeatRow parent;
    public Phrase phrase = null;

    public GameObject selectedHalo;

    // Start is called before the first frame update
    void Awake()
    {
        rend = bg.GetComponent<SpriteRenderer>();

        if (transform.parent != null)
            parent = transform.parent.GetComponent<BeatRow>();

        phrase = new NonePhrase(0); // Random null phrase
        updateGraphics();

    }

    private void Update()
    {
        selectedHalo.SetActive(MapEditor.sing.selectedPhraseSlot == this);
    }

    private void updateGraphics()
    {
        Color newCol = Color.white;

        if (phrase != null) {
            switch (phrase.type)
            {
                case Phrase.TYPE.NOTE:
                case Phrase.TYPE.ZIGZAG:
                case Phrase.TYPE.MANY:
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
        // Mark a change if the phrase is different
        if (phrase == null || !phrase.Equals(p))
        {
            MapEditor.sing.markChange();
        }

        setPhraseNoHotswap(p);
    }

    public void setPhraseNoHotswap(Phrase p)
    {
        phrase = p;

        updateGraphics();
    }

    int Clickable.onOver()
    {
        // Don't block if scrolling or zooming (is this hacky?)
        if (Input.mouseScrollDelta.y != 0)
            return 1;

        if (Input.GetKeyDown(MapEditor.sing.copyKey))
        {
            // Write phrase to active phrase
            MapEditor.sing.setActivePhrase(phrase.clone());
        }

        return 0;
    }

    public int onClick(int code)
    {
        // Set active phrase as this
        if (MapEditor.sing.InteractMode == MapEditor.MODE.EDIT)
        {
            MapEditor.sing.setActivePhrase(phrase.clone());
            MapEditor.sing.selectedPhraseSlot = this;
            MapEditor.sing.dragging = true;

            Debug.Log(phrase.ToString());

            return 0; // Catches input
        }

        return 1; // Lets input through if not editing
    }
    public void unsubSlot()
    {
        parent.slots.Remove(this);
        transform.parent = null;

        parent.desIfEmpty();
        parent.regenerate();

        parent = null;
    }

    public void subSlot(BeatRow par)
    {
        transform.parent = par.transform;
        parent = par;
    }
}
