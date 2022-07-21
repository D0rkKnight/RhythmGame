using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PhraseWorkspace : MonoBehaviour, Scrollable
{
    public GameObject beatMarkerPrefab;
    public GameObject beatEntryPrefab;
    public float height;

    private List<GameObject> beatMarkers = new List<GameObject>();
    public float beatsPerInterval = 2f;
    public float beatHeight = 1f;
    public float beatSnap = 0.25f;
    public float scroll = 0f;

    // Start is called before the first frame update
    void Start()
    {
        if (height <= 0) height = transform.Find("BG").localScale.y;

        regenBeatMarkers();
    }

    // Update is called once per frame
    void Update()
    {
        updatePhraseEntries();
    }

    public void regenBeatMarkers()
    {
        float intervalHeight = beatHeight * beatsPerInterval;

        // Clamp displayed bars to a range
        int intervalPow = 0; // 2^0 positive means intervals represent big beats
        while (intervalHeight > 2f)
        {
            intervalHeight /= 2;
            intervalPow--;
        }
        while (intervalHeight < 0.9f)
        {
            intervalHeight *= 2;
            intervalPow++;
        }

        // Set input fidelity to be half the interval height
        beatSnap = beatsPerInterval * Mathf.Pow(2, intervalPow) / 2;

        // Generate enough bars to fill the range
        while (beatMarkers.Count < height / intervalHeight + 2)
            beatMarkers.Add(Instantiate(beatMarkerPrefab, transform));

        for (int i = 0; i < beatMarkers.Count; i++)
        {
            GameObject marker = beatMarkers[i];

            // Get parity for opacity
            float scrollOff = intervalHeight - scroll % intervalHeight;

            marker.transform.localPosition = Vector3.down * (intervalHeight * i + scrollOff);
            float trueAlt = scroll + scrollOff + i * intervalHeight;

            float par = scroll % (2 * intervalHeight);
            bool weighted =  par > intervalHeight ^ i%2 == 0; // Checks for indexed parity and starting parity

            // Set opacity
            Color col = marker.GetComponent<SpriteRenderer>().color;
            marker.GetComponent<SpriteRenderer>().color = new Color(col.r, col.g, col.b, weighted ? 1.0f : 0.5f);

            // Set beat text
            float beat = trueAlt / beatHeight;
            marker.transform.Find("Canvas/BeatMarker").GetComponent<TMP_Text>().text = "" + beat;

            // Deactivate out of bounds markers
            float markerY = marker.transform.localPosition.y;
            marker.SetActive(-markerY <= height && markerY <= 0);
        }
    }

    public void updatePhraseEntries()
    {
        foreach (BeatRow entry in MapEditor.sing.phraseEntries)
        {
            // Set right altitude
            entry.transform.localPosition =
                Vector3.down * (entry.slots[0].phrase.beat * beatHeight - scroll);

            float entryY = entry.transform.localPosition.y;
            entry.gameObject.SetActive(-entryY <= height && entryY <= 0);
        }
    }

    public void addPhraseEntry()
    {
        // Get mouse altitude
        float mAlt = -(Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).y
            + scroll;

        float unroundBeat = mAlt / beatHeight;
        float beat = Mathf.Round(unroundBeat / beatSnap) * beatSnap;

        Phrase p = MapEditor.sing.activePhrase.clone();
        p.beat = beat;

        addPhraseEntry(p);
    }

    public void addPhraseEntry(Phrase p)
    {
        BeatRow br = Instantiate(beatEntryPrefab, transform.Find("Canvas")).GetComponent<BeatRow>();
        MapEditor.sing.phraseEntries.Add(br);

        br.setPhrase(p);

        updatePhraseEntries();
    }

    public void ScrollBy(float amt)
    {
        // Determine if scaled scroll or vertical scroll
        if (Input.GetKey(KeyCode.LeftControl))
        {
            float oldBH = beatHeight;
            beatHeight *= (1 + amt * 0.2f);

            // Change scroll as well to center around the mouse position
            float scrollAnchor = scroll - transform.InverseTransformPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition)).y;
            float anchorBeat = scrollAnchor / oldBH;
            float newScrollAnchor = anchorBeat * beatHeight;
            scroll += newScrollAnchor - scrollAnchor;
        } else
        {
            scroll -= amt;
            scroll = Mathf.Max(0, scroll);
        }

        regenBeatMarkers();
        updatePhraseEntries();
    }
}
