using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{

    public class NoteObj
    {
        public GameObject gameObj;
        public float hitTime; // In seconds, as Unity standard
        public Column col;

        public NoteObj(GameObject gameObj_, float hitTime_, Column col_)
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
    private float songStart;
    private float songStartDelay = 3f;

    public float travelSpeed = 5; // In Unity units per second
    public Vector2 dir = new Vector2(0, -1);
    public float hitWindow = 0.5f;

    public float noteAdvance = 5f;
    public float noteTimeout = 3f;

    public Text scoreText;
    public int score = 0;

    private List<NoteObj> notes;
    private List<NoteSerializer.Map.Note> noteQueue;

    // Start is called before the first frame update
    void Start()
    {
        notes = new List<NoteObj>();
        noteQueue = new List<NoteSerializer.Map.Note>();

        scoreText.text = "0";
        beatInterval = (float) 60.0 / bpm;
        lastBeat = Time.time;
        songStart = Time.time + songStartDelay;
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

        // Load in ready notes
        List<NoteSerializer.Map.Note> dump = new List<NoteSerializer.Map.Note>();
        foreach (NoteSerializer.Map.Note n in noteQueue)
        {
            float bTime = songStart + (beatInterval * n.beat);
            
            if (Time.time + noteAdvance > bTime)
            {
                // Load in note
                GameObject nObj = Instantiate(notePrefab);
                Column col = columns[n.lane];

                notes.Add(new NoteObj(nObj, bTime, col));

                dump.Add(n);
            }
        }

        foreach (NoteSerializer.Map.Note n in dump) noteQueue.Remove(n);
        dump.Clear();


        // Kill passed notes
        List<NoteObj> passed = new List<NoteObj>();

        foreach (NoteObj note in notes)
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
        foreach (NoteObj note in passed) kill(note);

        // Input
        foreach (Column col in columns)
        {
            if (Input.GetKeyDown(col.key))
            {
                // Get bets note within the acceptable input range
                NoteObj bestNote = null;

                foreach (NoteObj note in notes)
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

    private void kill(NoteObj note)
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
        // notes.Add(new NoteObj(Instantiate(notePrefab), Time.time + 5f, columns[0]));
    }

    private void highlightCol(Column col, Color c)
    {
        Transform tBox = col.gObj.transform.Find("TriggerBox");
        SpriteRenderer rend = tBox.GetComponent<SpriteRenderer>();
        rend.color = c;
    }

    public void enqueueNote(NoteSerializer.Map.Note note)
    {
        noteQueue.Add(note);
    }
}
