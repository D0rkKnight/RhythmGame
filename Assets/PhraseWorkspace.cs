using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhraseWorkspace : MonoBehaviour, Scrollable
{
    public GameObject beatMarkerPrefab;
    public GameObject beatEntryPrefab;
    public float height;

    private List<GameObject> beatMarkers = new List<GameObject>();
    public float intervalHeight = 2f;
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
        while (beatMarkers.Count < height / intervalHeight + 2)
            beatMarkers.Add(Instantiate(beatMarkerPrefab, transform));

        for (int i = 0; i < beatMarkers.Count; i++)
        {
            GameObject marker = beatMarkers[i];

            float scrollOff = scroll % intervalHeight;
            marker.transform.localPosition = Vector3.down * (intervalHeight * i + scrollOff);

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
        scroll -= amt;
        scroll = Mathf.Max(0, scroll);

        regenBeatMarkers();
        updatePhraseEntries();
    }
}
