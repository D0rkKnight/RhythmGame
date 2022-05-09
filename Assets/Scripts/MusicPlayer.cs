using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{
    /*public class NoteObj
    {
        public GameObject gameObj;
        public float hitTime; // In seconds, as Unity standard
        public float beat; // Beat for hit
        public Column lane;
        public bool dead;

        public NoteObj(GameObject gameObj_, float beat_, float hitTime_, Column col_)
        {
            gameObj = gameObj_;
            beat = beat_;
            hitTime = hitTime_;
            lane = col_;
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
    }*/

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
    public bool streamNotes = true;
    public float scroll = 0f; // Custom time offset 

    public float noteAdvance = 5f;
    public float noteTimeout = 3f;

    public Text scoreText;
    public int score = 0;

    private List<Note> notes;
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

        notes = new List<Note>();
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
        // This value anchors a lot of things
        songTime = Time.time - songStart - pausedTotal + scroll;
        currBeat = songTime / beatInterval;

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

        processPhraseQueue();

        // Kill passed notes
        List<Note> passed = new List<Note>();

        foreach (Note note in notes)
        {
            updateNote(note, passed);
        }

        // Clean dead note buffer
        foreach (Note note in passed) if (streamNotes) kill(note);

        // Input
        foreach (Column col in columns)
        {
            if (InputManager.checkKeyDown(col.key))
            {
                // Get best note within the acceptable input range
                Note bestNote = null;

                foreach (Note note in notes)
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
                    if (bestNote is HoldNote)
                    {
                        ((HoldNote)bestNote).held = true;
                        bestNote.highlight(Color.white);
                    }
                    else // Kill if regular note
                    {
                        if (streamNotes) kill(bestNote);
                        else bestNote.dead = true;
                    }
                }

                // Highlight BG
                NoteColumn colComp = col.gObj.GetComponent<NoteColumn>();
                colComp.highlight = 1;

            }

            if (Input.GetKeyUp(col.key))
            {
                // Mark held holds as dead
                foreach (Note n in notes)
                {
                    if (n is HoldNote && n.lane.Equals(col) && ((HoldNote)n).held)
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
        if (InputManager.checkKeyDown(resetKey))
        {
            resetSongEnv();
            MapSerializer.sing.playMap(); // Same behavior as end of song
        }

        // State transitions
        if (InputManager.checkKeyDown(pauseKey))
        {
            pause();
        }
    }

    private void statePause()
    {
        // Local song time + scroll delta
        // Write some kinda method that simplifies this...
        songTime = pauseStart-songStart-pausedTotal + scroll;
        currBeat = songTime / beatInterval;

        // Load in phrases that might be hotswapped
        processPhraseQueue();

        // Draw notes
        foreach (Note n in notes) updateNote(n, null);

        // State transitions
        if (InputManager.checkKeyDown(pauseKey))
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

        // Regenerate dead notes if scrolled past
        foreach (Note n in notes)
        {
            if (n.dead && n.beat > currBeat) n.dead = false;
        }
    }

    public void pause()
    {
        if (state != STATE.PAUSE) pauseStart = Time.time;
        state = STATE.PAUSE;
        TrackPlayer.sing.audio.Stop();

    }

    public void resetSongEnv()
    {
        songStart = Time.time + songStartDelay;
        pausedTotal = 0;
        scroll = 0;

        // Clear col blocks
        foreach (Column col in columns) col.blockedTil = 0;

        clearNotes();
        clearPhraseQueue();

        // Any audio we'd be playing would be illegal
        TrackPlayer.sing.audio.Stop();
    }

    public void clearNotes()
    {
        // Clear out note queue and active notes
        foreach (Note n in notes)
        {
            Destroy(n.gameObject);
        }
        notes.Clear();
    }
    public void clearPhraseQueue()
    {
        phraseQueue.Clear();
    }

    private void kill(Note note)
    {
        notes.Remove(note);
        Destroy(note.gameObject);
    }

    private void addScore(int amount)
    {
        score += amount;
        scoreText.text = "" + score;
    }

    private void onBeat()
    {
        // Handle holds
        foreach (Note n in notes)
        {
            if (n is HoldNote)
            {
                HoldNote hn = (HoldNote)n;

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
        Note nObj = Instantiate(notePrefab).GetComponent<Note>();
        nObj.lane = columns[lane];
        nObj.beat = beat;
        nObj.hitTime = bTime;

        notes.Add(nObj);
    }

    public void spawnHold(int lane, float beat, float holdLen)
    {
        if (!noteValid(lane, beat, holdLen))
        {
            Debug.LogWarning("Note spawn blocked at " + lane + ", " + beat);
            return;
        }

        float bTime = beatInterval * beat;

        HoldNote nObj = Instantiate(holdPrefab).GetComponent<HoldNote>();
        Transform bg = nObj.transform.Find("HoldBar");

        // Scale background bar appropriately
        bg.localScale = new Vector3(bg.localScale.x, travelSpeed * beatInterval * holdLen, 
            bg.localScale.z);

        nObj.lane = columns[lane];
        nObj.beat = beat;
        nObj.hitTime = bTime;

        notes.Add(nObj);
    } 

    private void updateNote(Note note, List<Note> passed)
    {
        GameObject col = note.lane.gObj;
        Vector2 tPos = col.transform.Find("TriggerBox").position;

        float dt = note.hitTime - songTime;

        Vector2 dp = -dir * dt * travelSpeed;
        Vector2 p = tPos + dp;
        note.transform.position = new Vector3(p.x, p.y, -1);

        if (passed != null)
        {
            float noteExtension = 0;
            if (note is HoldNote) noteExtension += ((HoldNote)note).holdBeats * beatInterval;
            if (dt < -(noteTimeout + noteExtension)) passed.Add(note);
        }
    }

    private void processPhraseQueue()
    {
        List<Phrase> dump = new List<Phrase>();
        foreach (Phrase p in phraseQueue)
        {
            float bTime = beatInterval * p.beat;

            if (songTime + noteAdvance > bTime || !streamNotes)
            {
                MapSerializer.sing.spawnNotes(p);
                dump.Add(p);
            }
        }

        foreach (Phrase n in dump) phraseQueue.Remove(n);
        dump.Clear();
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

        foreach (Note n in notes)
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
