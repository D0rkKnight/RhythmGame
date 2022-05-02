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
        public float beat; // Beat for hit
        public Column lane;
        public bool dead;

        public NoteObj(GameObject gameObj_, float beat_, float hitTime_, Column col_)
        {
            this.gameObj = gameObj_;
            beat = beat_;
            this.hitTime = hitTime_;
            this.lane = col_;
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

        public HoldObj(GameObject gameObj_, float beat_, float hitTime_, Column col_, float holdBeats_) 
            : base(gameObj_, beat_, hitTime_, col_)
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
    private float bpm = 60;
    public float BPM
    {
        get { return bpm; }
        set { 
            bpm = value;
            beatInterval = (float) 60.0 / value;
        }
    }

    private float beatInterval;
    private float currBeat;
    private float lastBeat;
    private float songStart;
    private float songStartDelay = 3f;
    private float songTime; // Time in seconds progressed through the song

    public float travelSpeed = 5; // In Unity units per second
    public Vector2 dir = new Vector2(0, -1);
    public float hitWindow = 0.5f;

    public float noteAdvance = 5f;
    public float noteTimeout = 3f;

    public Text scoreText;
    public int score = 0;

    private List<NoteObj> notes;
    private List<Phrase> phraseQueue;

    // Pause functionality
    public enum STATE
    {
        RUN, PAUSE
    }

    public KeyCode pauseKey = KeyCode.P;
    private float pauseStart = 0f;
    private float pausedTotal;
    public STATE state = STATE.RUN;

    public KeyCode resetKey = KeyCode.R;

    public static MusicPlayer sing;

    // Start is called before the first frame update
    void Start()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        notes = new List<NoteObj>();
        phraseQueue = new List<Phrase>();

        scoreText.text = "0";
        beatInterval = (float) 60.0 / bpm;
        lastBeat = Time.time;
        songStart = Time.time + songStartDelay;

        // Set activeness of columns
        columns[0].Active = true;
        columns[1].Active = true;
        columns[2].Active = true;
        columns[3].Active = true;

        SkillTree.sing.compile();
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case STATE.RUN:
                stateRun();
                break;
            case STATE.PAUSE:
                statePause();
                break;
            default:
                Debug.LogError("Illegal game state");
                break;
        }
    }

    private void stateRun()
    {
        songTime = Time.time - songStart - pausedTotal;
        currBeat = (Time.time - songStart) / beatInterval;

        // Start song if ready
        if (songTime >= 0 && !TrackPlayer.sing.audio.isPlaying)
        {
            TrackPlayer.sing.play();
            TrackPlayer.sing.audio.time = songTime;
        }

        // If eclipsing the beat threshold, tick a beat.
        if (Time.time > lastBeat + beatInterval)
        {
            lastBeat += beatInterval;
            onBeat();
        }

        List<Phrase> dump = new List<Phrase>();
        foreach (Phrase p in phraseQueue)
        {
            float bTime = beatInterval * p.beat;

            if (songTime + noteAdvance > bTime)
            {
                MapSerializer.sing.spawnNotes(p);
                dump.Add(p);
            }
        }

        foreach (Phrase n in dump) phraseQueue.Remove(n);
        dump.Clear();


        // Kill passed notes
        List<NoteObj> passed = new List<NoteObj>();

        foreach (NoteObj note in notes)
        {
            GameObject col = note.lane.gObj;
            Vector2 tPos = col.transform.Find("TriggerBox").position;

            float dt = note.hitTime - songTime;

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
                    if (!col.Equals(note.lane)) continue;

                    if (Mathf.Abs(note.hitTime - songTime) < hitWindow)
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
                foreach (NoteObj n in notes)
                {
                    if (n is HoldObj && n.lane.Equals(col) && ((HoldObj)n).held)
                    {
                        n.dead = true;
                        n.highlight(Color.grey);
                    }
                }
            }
        }

        // If no notes left, request note serializer to send more notes
        if (notes.Count == 0 && phraseQueue.Count == 0 && !MapSerializer.sing.loadQueued)
        {
            resetSongEnv();
            MapSerializer.sing.playMap();
        }

        // Reset key
        if (Input.GetKeyDown(resetKey))
        {
            resetSongEnv();
            MapSerializer.sing.playMap(); // Same behavior as end of song
        }

        // State transitions
        if (Input.GetKeyDown(pauseKey))
        {
            state = STATE.PAUSE;
            pauseStart = Time.time;
            TrackPlayer.sing.audio.Stop();
        }
    }

    private void statePause()
    {
        // State transitions
        if (Input.GetKeyDown(pauseKey))
        {
            state = STATE.RUN;
            pausedTotal += Time.time - pauseStart;

            // Resync music (if music is to be played)
            if (songTime >= 0)
            {
                TrackPlayer.sing.play();
                TrackPlayer.sing.audio.time = songTime;
            }
        }
    }

    public void resetSongEnv()
    {
        songStart = Time.time + songStartDelay;
        pausedTotal = 0;

        // Clear col blocks
        foreach (Column col in columns) col.blockedTil = 0;

        // Clear out note queue and active notes
        foreach (NoteObj n in notes)
        {
            Destroy(n.gameObj);
        }
        notes.Clear();
        phraseQueue.Clear();

        // Any audio we'd be playing would be illegal
        TrackPlayer.sing.audio.Stop();
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
                if (Mathf.Abs(hn.hitTime - songTime) < hitWindow) continue;

                // If still within point range
                if (hn.held && songTime < hn.hitTime + (hn.holdBeats * beatInterval))
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

    public void enqueuePhrase(Phrase phrase)
    {
        phraseQueue.Add(phrase);
    }

    public void spawnNote(int lane, float beat)
    {
        if (!noteValid(lane, beat, -1))
        {
            Debug.LogWarning("Note spawn blocked at " + lane + ", " + beat);
            return;
        }

        // Load in note
        float bTime = beatInterval * beat;
        GameObject nObj = Instantiate(notePrefab);
        Column col = columns[lane];

        notes.Add(new NoteObj(nObj, beat, bTime, col));
    }

    public void spawnHold(int lane, float beat, float holdLen)
    {
        if (!noteValid(lane, beat, holdLen))
        {
            Debug.LogWarning("Note spawn blocked at " + lane + ", " + beat);
            return;
        }

        float bTime = beatInterval * beat;

        GameObject nObj = Instantiate(holdPrefab);
        Transform bg = nObj.transform.Find("HoldBar");

        // Scale background bar appropriately
        bg.localScale = new Vector3(bg.localScale.x, travelSpeed * beatInterval * holdLen, 
            bg.localScale.z);

        Column col = columns[lane];
        notes.Add(new HoldObj(nObj, beat, bTime, col, holdLen));
    } 

    private bool noteValid(int lane, float beat, float blockDur)
    {
        Column col = columns[lane];


        if (beat < col.blockedTil)
        {
            Debug.LogWarning("Spawning a note in a blocked segment: beat "
                + beat + " when blocked til " + col.blockedTil);

            return false;
        }

        foreach (NoteObj n in notes)
        {
            if (n.beat == beat && n.lane == col)
            {
                Debug.LogWarning("Lane " + lane + " occupied by another note on same frame");
                return false;
            }
        }

        if (!col.Active)
        {
            Debug.LogWarning("Lane " + lane + " deactivated");
            return false;
        }

        if (blockDur > 0)
        {
            // Update column blocking
            col.blockedTil = Mathf.Max(col.blockedTil, beat + blockDur);
        }

        return true;
    }
}
