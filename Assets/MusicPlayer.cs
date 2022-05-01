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
        public bool dead;

        public NoteObj(GameObject gameObj_, float hitTime_, Column col_)
        {
            this.gameObj = gameObj_;
            this.hitTime = hitTime_;
            this.col = col_;
            dead = false;
        }

        public virtual void highlight(Color c)
        {
            gameObj.GetComponent<SpriteRenderer>().color = c;
        }
    }

    public class HoldObj : NoteObj
    {
        public float holdBeats;
        public bool held;

        public HoldObj(GameObject gameObj_, float hitTime_, Column col_, float holdBeats_) : base(gameObj_, hitTime_, col_)
        {
            holdBeats = holdBeats_;
            held = false;
        }

        public override void highlight(Color c)
        {
            base.highlight(c);
            Transform bg = gameObj.transform.Find("HoldBar");
            bg.GetComponent<SpriteRenderer>().color = c;
        }
    }

    [System.Serializable]
    public class Column
    {
        public GameObject gObj;
        public KeyCode key;
        public float blockedTil; // in beats

        private bool active = true;
        public bool Active
        {
            get { return active; }
            set
            {
                active = value;
                gObj.SetActive(value);
            }
        }

        public Column(GameObject gObj_, KeyCode key_, bool active_)
        {
            this.gObj = gObj_;
            this.key = key_;
            blockedTil = 0;
            Active = active_;
        }
    }

    public GameObject notePrefab;
    public GameObject holdPrefab;
    public Column[] columns;
    public float bpm = 60;
    private float beatInterval;
    private float currBeat;
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
    private List<MapSerializer.Map.Note> noteQueue;

    public static MusicPlayer sing;

    // Start is called before the first frame update
    void Start()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        notes = new List<NoteObj>();
        noteQueue = new List<MapSerializer.Map.Note>();

        scoreText.text = "0";
        beatInterval = (float) 60.0 / bpm;
        lastBeat = Time.time;
        songStart = Time.time + songStartDelay;

        // Set activeness of columns
        columns[0].Active = false;
        columns[1].Active = true;
        columns[2].Active = true;
        columns[3].Active = false;
    }

    // Update is called once per frame
    void Update()
    {
        currBeat = (Time.time - songStart) / beatInterval;

        // Start song if ready
        if (Time.time > songStart && !TrackPlayer.sing.audio.isPlaying)
        {
            TrackPlayer.sing.play();
        }

        // If eclipsing the beat threshold, spawn a beat.
        if (Time.time > lastBeat + beatInterval)
        { 
            lastBeat += beatInterval;
            onBeat();
        }

        // Load in ready notes
        List<MapSerializer.Map.Note> dump = new List<MapSerializer.Map.Note>();
        foreach (MapSerializer.Map.Note n in noteQueue)
        {
            float bTime = songStart + (beatInterval * n.beat);
            
            if (Time.time + noteAdvance > bTime)
            {
                if (n.hold) spawnHold(n);
                else spawnNote(n);
                dump.Add(n);
            }
        }

        foreach (MapSerializer.Map.Note n in dump) noteQueue.Remove(n);
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

            float noteExtension = 0;
            if (note is HoldObj) noteExtension += ((HoldObj)note).holdBeats * beatInterval;
            if (dt < -(noteTimeout + noteExtension)) passed.Add(note);
        }

        // Clean dead note buffer
        foreach (NoteObj note in passed) kill(note);

        // Input
        foreach (Column col in columns)
        {
            if (Input.GetKeyDown(col.key))
            {
                // Get best note within the acceptable input range
                NoteObj bestNote = null;

                foreach (NoteObj note in notes)
                {
                    if (note.dead) continue;
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
                    if (bestNote is HoldObj)
                    {
                        ((HoldObj)bestNote).held = true;
                        bestNote.highlight(Color.white);
                    }
                    else // Kill if regular note
                    {
                        kill(bestNote);
                    }
                }

                // Highlight trigger box
                highlightCol(col, Color.yellow);

            }

            if (Input.GetKeyUp(col.key))
            {
                // Reset color
                highlightCol(col, Color.white);

                // Mark held holds as dead
                foreach(NoteObj n in notes) {
                    if (n is HoldObj && n.col.Equals(col) && ((HoldObj) n).held)
                    {
                        n.dead = true;
                        n.highlight(Color.grey);
                    }
                }
            }
        }

        // If no notes left, request note serializer to send more notes
        if (notes.Count == 0 && noteQueue.Count == 0 && !MapSerializer.sing.loadQueued)
        {
            songStart = Time.time + songStartDelay;

            // Clear col blocks
            foreach (Column col in columns) col.blockedTil = 0;

            MapSerializer.sing.genMap();
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
        // Handle holds
        foreach (NoteObj n in notes)
        {
            if (n is HoldObj)
            {
                HoldObj hn = (HoldObj)n;

                // Don't give ticking when first hit
                if (Mathf.Abs(hn.hitTime - Time.time) < hitWindow) continue;

                // If still within point range
                if (hn.held && Time.time < hn.hitTime + (hn.holdBeats * beatInterval))
                {
                    addScore(10);
                }
            }
        }

    }

    private void highlightCol(Column col, Color c)
    {
        Transform tBox = col.gObj.transform.Find("TriggerBox");
        SpriteRenderer rend = tBox.GetComponent<SpriteRenderer>();
        rend.color = c;
    }

    public void enqueueNote(MapSerializer.Map.Note note)
    {
        noteQueue.Add(note);
    }

    private void spawnNote(MapSerializer.Map.Note n)
    {
        // Load in note
        float bTime = songStart + (beatInterval * n.beat);
        GameObject nObj = Instantiate(notePrefab);
        Column col = columns[n.lane];

        notes.Add(new NoteObj(nObj, bTime, col));
    }

    private void spawnHold(MapSerializer.Map.Note n)
    {
        if (!n.hold) Debug.LogError("Data pack does not designate a hold");

        float bTime = songStart + (beatInterval * n.beat);

        GameObject nObj = Instantiate(holdPrefab);
        Transform bg = nObj.transform.Find("HoldBar");

        // Scale background bar appropriately
        bg.localScale = new Vector3(bg.localScale.x, travelSpeed * beatInterval * n.holdLen, bg.localScale.z);

        Column col = columns[n.lane];
        notes.Add(new HoldObj(nObj, bTime, col, n.holdLen));
    } 
}
