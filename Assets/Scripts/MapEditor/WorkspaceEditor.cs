using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WorkspaceEditor : MonoBehaviour, Scrollable
{
    public GameObject beatMarkerPrefab;
    public GameObject beatEntryPrefab;
    public GameObject editorSlotPrefab;

    public GameObject ghost;
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

        BeatRow ghostRow = ghost.GetComponent<BeatRow>();
        ghostRow.addPhrase(MapEditor.sing.activePhrase.hardClone());
        ghostRow.slots[0].enabled = false;

        SpriteRenderer slotRend = ghostRow.slots[0].GetComponentInChildren<SpriteRenderer>();
        Color slotCol = slotRend.color;
        slotCol.a = 0.5f;
        slotRend.color = slotCol;

        regenBeatMarkers();
    }

    // Update is called once per frame
    void Update()
    {
        updatePhraseEntries();

        // Just sending it with the maths
        float snapInterval = beatSnap * beatHeight;

        float mouseAlt = Camera.main.ScreenToWorldPoint(Input.mousePosition).y - transform.position.y;
        float mouseBar = (mouseAlt - scroll % snapInterval) / snapInterval; // Transform to bar offset from first bar
        float snapAlt = Mathf.Round(mouseBar) * snapInterval + (scroll % snapInterval);
        float snapBeat = (-snapAlt + scroll) / beatHeight;

        // Hella boilerplate lol
        Vector3 newPos = ghost.transform.localPosition;
        newPos.y = snapAlt;
        ghost.transform.localPosition = newPos;

        // Set ghost phrase
        BeatRow row = ghost.GetComponent<BeatRow>();
        Phrase newPhrase = MapEditor.sing.activePhrase.hardClone();
        newPhrase.beat = snapBeat;

        row.slots[0].setPhraseNoHotswap(newPhrase);
        row.regenerate();

        float ghostY = ghost.transform.localPosition.y;
        ghost.SetActive(MapEditor.sing.InteractMode == MapEditor.MODE.WRITE &&
            -ghostY <= height && ghostY <= 0);

        if (MapEditor.sing.draggingPhraseSlot)
        {
            // Move around the slot
            BeatEditorSlot slot = MapEditor.sing.selectedPhraseSlot;

            if (slot != null && snapBeat != slot.phrase.beat)
            {
                slot.unsubSlot();


                Phrase slotP = slot.phrase.fullClone();
                slotP.beat = snapBeat;
                slot.setPhrase(slotP); // Set new beat

                MapEditor.sing.setActivePhrase(slotP); // Copy back into active phrase

                // Add to new slot
                addPhraseEntry(slot);
            }
        }

        // Regenerate phrase group data
        MapEditor.sing.workspace.group.phrases = rowsToPhraseListing(MapEditor.sing.workspace.rows);
    }

    public void regenBeatMarkers()
    {
        float intervalHeight = getIntervalHeight();

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

            marker.transform.localPosition = Vector3.down * (intervalHeight * i + scrollOff) +
                Vector3.right * transform.Find("BG").localScale.x;
            float trueAlt = scroll + scrollOff + i * intervalHeight;

            float par = scroll % (2 * intervalHeight);
            bool weighted =  par > intervalHeight ^ i%2 == 0; // Checks for indexed parity and starting parity

            // Set opacity
            Transform line = marker.transform.Find("Line");
            Color col = line.GetComponent<SpriteRenderer>().color;
            line.GetComponent<SpriteRenderer>().color = new Color(col.r, col.g, col.b, weighted ? 1.0f : 0.5f);

            // Set beat text
            float beat = trueAlt / beatHeight;
            marker.transform.Find("Canvas/BeatMarker").GetComponent<TMP_Text>().text = "" + beat;

            // Deactivate out of bounds markers
            float markerY = marker.transform.localPosition.y;
            marker.SetActive(-markerY <= height && markerY <= 0);
        }
    }

    public float getIntervalHeight()
    {
        return beatHeight * beatsPerInterval;
    }

    public void updatePhraseEntries()
    {
        foreach (BeatRow entry in MapEditor.sing.workspace.rows)
        {
            // Set right altitude
            entry.transform.localPosition =
                Vector3.down * (entry.slots[0].phrase.beat * beatHeight - scroll) + Vector3.back * 3;

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

        Phrase p = MapEditor.sing.activePhrase.hardClone();
        p.beat = beat;

        addPhraseEntry(p);
    }

    public void addPhraseEntry(Phrase p)
    {
        GameObject slotObj = Instantiate(editorSlotPrefab);
        BeatEditorSlot slot = slotObj.GetComponent<BeatEditorSlot>();

        slot.setPhrase(p);
        addPhraseEntry(slot);
    }

    public void addPhraseEntry(BeatEditorSlot slot)
    {
        BeatRow activeRow = null;
        foreach (BeatRow row in MapEditor.sing.workspace.rows)
        {
            if (row.slots[0].phrase.beat == slot.phrase.beat)
            {
                activeRow = row;
                break;
            }
        }

        if (activeRow == null)
        {
            activeRow = Instantiate(beatEntryPrefab, transform.Find("Canvas")).GetComponent<BeatRow>();
            MapEditor.sing.workspace.rows.Add(activeRow);
        }

        activeRow.addSlot(slot);

        // Resort workspace
        MapEditor.sing.workspace.sortRows();

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

        // Lock mp to scroll as well (in beats)
        // This is some weird math lol
        MusicPlayer mp = MusicPlayer.sing;
        float scrollessST = mp.songTime - mp.scroll;
        MusicPlayer.sing.scroll = scroll / beatHeight - scrollessST;

        regenBeatMarkers();
        updatePhraseEntries();
    }

    public void setScroll(float val)
    {
        scroll = val;

        regenBeatMarkers();
        updatePhraseEntries();
    }

    public List<Phrase> rowsToPhraseListing(List<BeatRow> rows)
    {
        List<Phrase> o = new List<Phrase>();
        foreach (BeatRow row in rows)
        {
            foreach (BeatEditorSlot slot in row.slots)
                o.Add(slot.phrase);
        }

        return o;
    }
}
