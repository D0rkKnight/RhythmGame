using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicPlayer : MonoBehaviour
{
    public Note notePrefab;
    public HoldNote holdPrefab;
    public ReboundNote reboundPrefab;
    public GhostNote ghostPrefab;

    public NoteColumn[] columns;
    private float bpm = 60;
    public float BPM
    {
        get { return bpm; }
        set { 
            bpm = value;
            beatInterval = (float) 60.0 / value;
        }
    }

    public float beatInterval;
    public float currBeat;
    public float lastBeat;
    public float songStart;
    public float songStartDelay = 3f;
    public float songTime; // Time in seconds progressed through the song

    public float travelSpeed = 5; // In Unity units per second
    public Vector2 dir = new Vector2(0, -1); // Direction of travel

    public float hitWindow = 0.5f;
    public float perfectWindow = 0.25f;
    public float missWindow = 1f; // Any press in this window that is not a hit is a miss

    public bool streamNotes = true;
    public float scroll = 0f; // Custom time offset 

    public float noteAdvance = 5f;
    public float noteTimeout = 3f;

    public Text scoreText;
    private int score = 0;
    public int Score
    {
        get { return score; }
        set
        {
            score = value;
            scoreText.text = value.ToString();
        }
    }
    public Scoreboard scoreboard;

    public List<Note> notes;
    public List<Phrase> phraseQueue;

    public float tpOffset = 0f; // # of seconds off the music is vs the game
                                // positive -> music plays first
    public float lagspikeTolerance = 0.05f;
    public bool showNoteWeight = false;

    // Combo
    public Text comboCounter;
    private float combo;
    public float Combo
    {
        get {
            if (!SkillTree.sing.activeFlags[(int)SkillTree.NODE.COMBO]) 
                return 0;

            return combo; 
        }
        set
        {
            combo = value;
            if (!SkillTree.sing.activeFlags[(int)SkillTree.NODE.COMBO])
                combo = 0;

            comboCounter.text = "x"+combo.ToString();
        }
    }

    // Pause functionality
    public enum STATE
    {
        RUN, PAUSE, INTERIM, SLEEPING
    }

    public KeyCode pauseKey = KeyCode.P;
    private float pauseStart = 0f;
    private float pausedTotal;
    public STATE state = STATE.SLEEPING;
    public bool pauseOnAwake = false;

    float interimTil = 0; // Deadline for interim period between songs
    public bool willUnpauseCD = true; // Countdown on unpause

    public GameObject accuracyPopupPrefab;
    public Transform accuracyPopupLoc;

    public KeyCode resetKey = KeyCode.R;

    public TMP_Text pauseText;

    public static MusicPlayer sing;

    // Start is called before the first frame update
    void Awake()
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
        foreach (NoteColumn c in columns)
        {
            c.Active = true;
            c.StreamOn = true;
        }
    }

    private void Start()
    {
        Combo = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate songtime before going in
        updateInternals()
            ;
        switch (state)
        {
            case STATE.RUN:
                stateRun();
                break;
            case STATE.PAUSE:
                statePause();
                break;
            case STATE.INTERIM:
                stateInter();
                break;
            case STATE.SLEEPING:
                stateSleep();
                break;
            default:
                Debug.LogError("Illegal game state");
                break;
        }
    }

    public void updateInternals()
    {
        switch (state)
        {
            case STATE.RUN:
                songTime = Time.time - songStart - pausedTotal + scroll;
                break;

            case STATE.PAUSE:
                songTime = pauseStart - songStart - pausedTotal + scroll;
                break;

        }

        currBeat = songTime / beatInterval;
    }

    private void stateRun()
    {
        // Start song if ready
        float tpTime = getTrackTime();
        if (tpTime >= 0 && !TrackPlayer.sing.audio.isPlaying)
        {
            TrackPlayer.sing.play();
            TrackPlayer.sing.setTime(tpTime); // Sync song time
        }

        // Resync on lagspike
        if (Time.deltaTime > lagspikeTolerance && tpTime >= 0 && TrackPlayer.sing.audio.isPlaying)
            TrackPlayer.sing.setTime(tpTime);

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
            note.updateNote(this, passed);
        }

        // Clean dead note buffer
        foreach (Note note in passed) if (streamNotes) remove(note);

        // Input
        foreach (NoteColumn col in columns)
        {
            if (InputManager.checkKeyDownGame(col.Key))
            {
                // Get best note within the acceptable input range
                Note bestNote = null;
                Note closestNote = null; // Could be dead already

                foreach (Note note in notes)
                {
                    // Check both assigned column and reroute for validity
                    if (!note.lane.Equals(col) && !note.lane.Equals(col.reroute)) continue;

                    // Grab closest note regardless of if it's alive or not
                    if (closestNote == null || 
                        Mathf.Abs(songTime - note.getHitTime()) < Mathf.Abs(songTime - closestNote.getHitTime()))
                        closestNote = note;

                    if (note.dead) continue;

                    if (bestNote == null || note.getHitTime() < bestNote.getHitTime()) // Hits earliest note
                        bestNote = note;
                }

                if (bestNote != null)
                {
                    if (Mathf.Abs(bestNote.getHitTime() - songTime) < hitWindow)
                    {
                        addNoteScore();
                        hit(bestNote);

                        // Check accuracy of hit
                        float delta = Mathf.Abs(bestNote.getHitTime() - songTime);
                        broadCastHitAcc(delta);
                    }

                    // Check if its a miss since it's not a hit
                    // Assume you were aiming for the closest note and check if it's outside of the hit window
                    // Means the player did not hit any note, but the closest note (the note they were aiming for) is also alive
                    // Which means they should be penalized
                    else if (!closestNote.dead && Mathf.Abs(closestNote.getHitTime() - songTime) < missWindow)
                    {
                        miss(bestNote);

                        float delta = Mathf.Abs(bestNote.getHitTime() - songTime);
                        broadCastHitAcc(delta);
                    }
                }

                // Highlight BG
                NoteColumn colComp = col.gameObject.GetComponent<NoteColumn>();
                colComp.highlight = 1;

            }
        }

        // Release behavior
        foreach (Note n in notes)
            if (Input.GetKeyUp(n.lane.Key))
            {
                n.onLaneRelease();
            }

        // If no notes left, request note serializer to send more notes
        if (notes.Count == 0 && phraseQueue.Count == 0 && !MapSerializer.sing.loadQueued)
        {
            inter();
        }

        // Reset key
        if (InputManager.checkKeyDownGame(resetKey))
        {
            resetSongEnv();
            MapSerializer.sing.playMap(); // Resets to start of the current map
        }

        // State transitions
        if (InputManager.checkKeyDownGame(pauseKey))
        {
            pause();
        }

        // Pause label is inactive
        pauseText.gameObject.SetActive(false);

        // Link w/ wEdit
        linkWEditScroll();
    }

    // Very local variables
    float unpauseCountSec = 3f;
    int unpauseCountSubdivisions = 4;
    float unpauseCountdown = -1000f;
    private void statePause()
    {
        // Load in phrases that might be hotswapped
        processPhraseQueue();

        // Set pause label
        pauseText.gameObject.SetActive(true);
        pauseText.text = "Press " + pauseKey.ToString() + " to Unpause";

        // Draw notes
        foreach (Note n in notes) n.updateNote(this, null);

        // State transitions
        if (InputManager.checkKeyDownGame(pauseKey))
        {
            if (willUnpauseCD)
            {
                float beatsExpected = Mathf.Ceil(unpauseCountSec / beatInterval);
                unpauseCountdown = (beatsExpected / unpauseCountSubdivisions + (currBeat % (1.0f/unpauseCountSubdivisions))) 
                                    * beatInterval;
            }
            else
                unpause();
        }

        // Unpause logic (either unpause is nonnegative and ticks, or is negative and is waiting for unpause)
        if (unpauseCountdown >= 0)
        {
            unpauseCountdown -= Time.deltaTime;
            if (unpauseCountdown < 0)
            {
                unpause();
            }
            else
            {
                float pBeatTime = unpauseCountdown/beatInterval - (currBeat % (1.0f / unpauseCountSubdivisions));
                int pauseVal = (int) Mathf.Ceil(pBeatTime * unpauseCountSubdivisions);

                pauseText.text = pauseVal > 0 ? ""+pauseVal : "";
            }
        }

        // Regenerate dead notes if scrolled past
        foreach (Note n in notes)
        {
            if (n.dead && n.getHitTime() > songTime)
            {
                n.dead = false;
                n.resetInit(this);
            }
        }
    }

    // Buffer period to avoid excessive flip flopping
    private void stateInter()
    {
        /*if (Time.time > interimTil)
        {
            resetSongEnv();
            Timeliner.sing.playNextMap();

            state = STATE.RUN;
        }*/

        scoreboard.footer.text = "Press " + pauseKey.ToString() + " to Play";

        // Can also force an awake by itself
        if (InputManager.checkKeyDownGame(pauseKey))
        {
            resetSongEnv();
            GameManager.sing.playNextMap();

            state = STATE.RUN;
        }
    }

    private void inter()
    {
        state = STATE.INTERIM;
        interimTil = Time.time + 1f;

        // Convert score to tokens
        if (SkillTree.sing.GetType() == typeof(MainSkillTree))
        {
            MainSkillTree mst = (MainSkillTree)SkillTree.sing;
            mst.SubToken += score / 2;
        }

        // Add score to highscore
        GameManager.sing.addScore(MapSerializer.sing.activeMap.name, Score);
        scoreboard.loadScores(MapSerializer.sing.activeMap.name);

        // Zero out other stuff
        Score = 0;
        Combo = 0;

        // Save game
        if (GameManager.sing.saveOnInter)
            GameManager.writeSave();
    }

    private void stateSleep()
    {
        pauseText.gameObject.SetActive(true);
        pauseText.text = "Press " + pauseKey.ToString() + " to Play";

        // Waits for a map to queue up, then resets the environment and pauses.
        if (MapSerializer.sing.activeMap != null)
        {
            resetSongEnv();
            MapSerializer.sing.playMap();

            if (pauseOnAwake)
                pause();
            else
                state = STATE.RUN;
        }

        // Can also force an awake by itself
        if (InputManager.checkKeyDownGame(pauseKey))
        {
            GameManager.sing.playNextMap();

            if (pauseOnAwake)
                pause();
            else
                state = STATE.RUN;
        }
    }

    public void pause()
    {
        if (state != STATE.PAUSE) pauseStart = Time.time;
        state = STATE.PAUSE;
        TrackPlayer.sing.audio.Stop();

        Debug.Log("Pausing");
    }

    public void unpause()
    {
        state = STATE.RUN;
        pausedTotal += Time.time - pauseStart;

        // Resync music (if music is to be played)
        float tpTime = getTrackTime();
        if (songTime >= 0 && tpTime > 0)
        {
            TrackPlayer.sing.play();
            TrackPlayer.sing.setTime(tpTime);
        }
    }

    public void resetSongEnv(bool resetTrackPos = true)
    {
        if (resetTrackPos)
        {
            songStart = Time.time + songStartDelay;
            pausedTotal = 0;
            scroll = 0;
        }

        clearNotes();
        clearPhraseQueue();

        // Any audio we'd be playing would be illegal
        TrackPlayer.sing.audio.Stop();
    }

    public void reloadNotes()
    {
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

    private void remove(Note note)
    {
        notes.Remove(note);
        note.remove();
    }

    private void hit(Note note)
    {
        note.hit(out bool remove);
        if (remove) {
            if (streamNotes)
            {
                notes.Remove(note);
                note.kill();
            }
            else note.dead = true;
        }

        note.lane.hitBurst();

        // Increment combo
        Combo++;

        // Gain heat
        HeatController.sing.Heat++;
    }

    public void miss(Note note)
    {
        note.dead = true;
        Combo = 0;

        // Lose heat
        HeatController.sing.Heat -= 5;
    }

    private void addNoteScore()
    {
        int amt = 100 * (int) (1 + combo / 10f);
        Score += amt;
        if (SkillTree.sing.GetType() == typeof(MainSkillTree))
            ((MainSkillTree) SkillTree.sing).SubToken += amt / 2; // Get tokens while playing too
    }

    private void onBeat()
    {
        // Handle holds
        foreach (Note n in notes)
        {
            n.onBeat(this);
        }

    }

    public void enqueuePhrase(Phrase phrase)
    {
        phraseQueue.Add(phrase);
    }

    public void addNote(Note n)
    {
        notes.Add(n);
        n.ShowWeight = showNoteWeight;
    }

    public void removeNote(Note n)
    {
        Destroy(n.gameObject);
        notes.Remove(n);
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

    public float getCurrBeat() { return currBeat; }

    public void broadCastHitAcc(float delta)
    {
        Transform canv = transform.Find("Canvas");
        GameObject popup = Instantiate(accuracyPopupPrefab, canv);
        popup.transform.position = accuracyPopupLoc.position;

        string txt;
        if (delta < perfectWindow)
        {
            // Perfect hit
            txt = "Perfect!";
        }
        else if (delta < hitWindow)
        {
            // Regular hit
            txt = "OK!";
        }
        else
        {
            // Miss
            txt = "Miss";
        }

        popup.GetComponent<Text>().text = txt;
    }

    public int getReroute(int col)
    {
        while (!columns[col].StreamOn && columns[col].defNoteReroute >= 0)
            col = columns[col].defNoteReroute;

        return col;
    }

    // Track time is the time the track player should be at, not the song time
    public float getTrackTime()
    {
        float tpTime = songTime + tpOffset + MapSerializer.sing.activeMap.offset;

        return tpTime;
    }

    public void scrollBy(float amt)
    {
        if (amt == 0) return;

        scroll += amt;
        updateInternals();

        // Also procs notes onscroll
        foreach (Note n in notes)
            n.onScroll(this);

        linkWEditScroll();
    }

    public void linkWEditScroll()
    {
        // Link scrolls w/ workspace
        if (MapEditor.sing != null)
        {
            WorkspaceEditor wEdit = MapEditor.sing.workspaceEditor;
            wEdit.setScroll(songTime / beatInterval * wEdit.beatHeight);
        }
    }
}
