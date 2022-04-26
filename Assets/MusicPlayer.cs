using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{

    public class NoteData
    {
        public GameObject gameObj;
        public float hitTime; // In seconds, as Unity standard
        public Column col;

        public NoteData(GameObject gameObj_, float hitTime_, Column col_)
        {
            this.gameObj = gameObj_;
            this.hitTime = hitTime_;
            this.col = col_;
        }
    }

    [System.Serializable]
    public struct Column
    {
        public GameObject gObj;
        public KeyCode key;

        public Column(GameObject gObj_, KeyCode key_)
        {
            this.gObj = gObj_;
            this.key = key_;
        }
    }

    public GameObject notePrefab;
    public Column[] columns;
    public float bpm = 60;
    private float beatInterval;
    private float lastBeat;

    public float travelSpeed = 5; // In Unity units per second
    public Vector2 dir = new Vector2(0, -1);
    public float hitWindow = 0.5f;

    public float noteTimeout = 3f;

    public Text scoreText;
    public int score = 0;

    private List<NoteData> notes;

    // Start is called before the first frame update
    void Start()
    {
        notes = new List<NoteData>();
        scoreText.text = "0";
        beatInterval = (float) 60.0 / bpm;
        lastBeat = Time.time;

    }

    // Update is called once per frame
    void Update()
    {
        // If eclipsing the beat threshold, spawn a beat.
        if (Time.time > lastBeat + beatInterval)
        {
            lastBeat += beatInterval;
            onBeat();
        }

        // Only run on channel 1


        // Kill passed notes
        List<NoteData> passed = new List<NoteData>();

        foreach (NoteData note in notes)
        {
            GameObject col = note.col.gObj;
            Vector2 tPos = col.transform.Find("TriggerBox").position;

            float dt = note.hitTime - Time.time;

            Vector2 dp = -dir * dt * travelSpeed;
            Vector2 p = tPos + dp;
            note.gameObj.transform.position = new Vector3(p.x, p.y, -1);

            if (dt < -noteTimeout) passed.Add(note);
        }

        // Clean dead note buffer
        foreach (NoteData note in passed) kill(note);

        // Input
        foreach (Column col in columns)
        {
            if (Input.GetKeyDown(col.key))
            {
                // Get bets note within the acceptable input range
                NoteData bestNote = null;

                foreach (NoteData note in notes)
                {
                    if (!col.Equals(note.col)) continue;

                    if (Mathf.Abs(note.hitTime - Time.time) < hitWindow)
                    {
                        if (bestNote == null || note.hitTime < bestNote.hitTime)
                            bestNote = note;
                    }
                }

                if (bestNote != null)
                {
                    addScore(100);
                    kill(bestNote);
                }

                // Highlight trigger box
                highlightCol(col, Color.yellow);

            }

            if (Input.GetKeyUp(col.key))
            {
                // Reset color
                highlightCol(col, Color.white);
            }
        }
    }

    private void kill(NoteData note)
    {
        notes.Remove(note);
        Destroy(note.gameObj);
    }

    private void addScore(int amount)
    {
        score += amount;
        scoreText.text = "" + score;
    }

    private void onBeat()
    {

        // Spawn some sample notes for testing
        notes.Add(new NoteData(Instantiate(notePrefab), Time.time + 5f, columns[0]));
    }

    private void highlightCol(Column col, Color c)
    {
        Transform tBox = col.gObj.transform.Find("TriggerBox");
        SpriteRenderer rend = tBox.GetComponent<SpriteRenderer>();
        rend.color = c;
    }

    //setNotePos(GameObject column, )
}
