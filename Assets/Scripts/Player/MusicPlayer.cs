﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{
    public GameObject notePrefab;
    public GameObject holdPrefab;
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
    public Vector2 dir = new Vector2(0, -1);

    public float hitWindow = 0.5f;
    public float perfectWindow = 0.25f;

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

    public List<Note> notes;
    public List<Phrase> phraseQueue;

    public float tpOffset = 1f; // # of seconds off the music is vs the game
                                // positive -> music plays first

    // Combo
    public Text comboCounter;
    private float combo;
    public float Combo
    {
        get { return combo; }
        set
        {
            combo = value;
            comboCounter.text = "x"+combo.ToString();
        }
    }

    // Pause functionality
    public enum STATE
    {
        RUN, PAUSE, INTERIM
    }

    public KeyCode pauseKey = KeyCode.P;
    private float pauseStart = 0f;
    private float pausedTotal;
    public STATE state = STATE.RUN;

    float interimTil = 0; // Deadline for interim period between songs

    public GameObject accuracyPopupPrefab;
    public Transform accuracyPopupLoc;

    public KeyCode resetKey = KeyCode.R;

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

        Combo = 0;
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
            case STATE.INTERIM:
                stateInter();
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
        float tpTime = songTime + tpOffset;
        if (tpTime >= 0 && !TrackPlayer.sing.audio.isPlaying)
        {
            TrackPlayer.sing.play();
            TrackPlayer.sing.audio.time = tpTime; // Sync song time
        }

        // Resync on lagspike
        if (Time.deltaTime > 0.01 && tpTime >= 0 && TrackPlayer.sing.audio.isPlaying)
            TrackPlayer.sing.audio.time = tpTime;

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
        foreach (Note note in passed) if (streamNotes) remove(note);

        // Input
        foreach (NoteColumn col in columns)
        {
            if (InputManager.checkKeyDown(col.Key))
            {
                // Get best note within the acceptable input range
                Note bestNote = null;

                foreach (Note note in notes)
                {
                    if (note.dead) continue;

                    // Check both assigned column and reroute for validity
                    if (!note.lane.Equals(col) && !note.lane.Equals(col.reroute)) continue;

                    if (Mathf.Abs(note.hitTime - songTime) < hitWindow)
                    {
                        if (bestNote == null || note.hitTime < bestNote.hitTime) // Hits first available note in the window
                            bestNote = note;
                    }
                }

                if (bestNote != null)
                {
                    addNoteScore();
                    if (bestNote is HoldNote)
                    {
                        ((HoldNote)bestNote).held = true;
                        bestNote.highlight(Color.white);
                    }
                    else // Kill if regular note
                    {
                        if (streamNotes) hit(bestNote);
                        else bestNote.dead = true;
                    }

                    // Check accuracy of hit
                    float delta = Mathf.Abs(bestNote.hitTime - songTime);
                    broadCastHitAcc(delta);
                }

                // Highlight BG
                NoteColumn colComp = col.gameObject.GetComponent<NoteColumn>();
                colComp.highlight = 1;

            }

            if (Input.GetKeyUp(col.Key))
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
            state = STATE.INTERIM;
            interimTil = Time.time + 1f;

            // Convert score to tokens
            if (SkillTree.sing.GetType() == typeof(MainSkillTree))
            {
                MainSkillTree mst = (MainSkillTree) SkillTree.sing;
                mst.SubToken += score / 2;
            }

            // Zero out other stuff
            Score = 0;
            Combo = 0;
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

    // Buffer period to avoid excessive flip flopping
    private void stateInter()
    {
        if (Time.time > interimTil)
        {
            resetSongEnv();
            MapSerializer.sing.playMap();

            state = STATE.RUN;
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
        resetColumnBlocking();

        clearNotes();
        clearPhraseQueue();

        // Any audio we'd be playing would be illegal
        TrackPlayer.sing.audio.Stop();
    }

    public void resetColumnBlocking()
    {
        foreach (NoteColumn col in columns) col.blockedTil = 0;
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
        notes.Remove(note);
        note.hit();
        note.lane.hitBurst();

        // Increment combo
        Combo++;

        // Gain heat
        HeatController.sing.Heat++;
    }

    private void miss(Note note)
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
            if (n is HoldNote)
            {
                HoldNote hn = (HoldNote)n;

                // Don't give ticking when first hit
                if (Mathf.Abs(hn.hitTime - songTime) < hitWindow) continue;

                // If still within point range
                if (hn.held && songTime < hn.hitTime + (hn.holdBeats * beatInterval))
                {
                    Score += 10;
                }
            }
        }

    }

    public void enqueuePhrase(Phrase phrase)
    {
        phraseQueue.Add(phrase);
    }

    public void addNote(Note n)
    {
        notes.Add(n);
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

        nObj.lane = columns[lane];
        nObj.beat = beat;
        nObj.hitTime = bTime;

        notes.Add(nObj);
    } 

    private void updateNote(Note note, List<Note> passed)
    {
        GameObject col = note.lane.gameObject;
        Vector2 tPos = col.transform.Find("TriggerBox").position;

        float dt = note.hitTime - songTime;

        Vector2 dp = -dir * dt * travelSpeed;
        Vector2 p = tPos + dp;
        note.transform.position = new Vector3(p.x, p.y, -1);

        // Check if strictly unhittable
        if (dt < -hitWindow && !note.dead)
        {
            miss(note);
        }

        // Sort into discard pile
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

    public bool noteValid(int lane, float beat, float blockDur)
    {
        if (lane < 0 || lane >= 4)
        {
            // Catch it
            Debug.Log("Catch");
        }

        NoteColumn col = columns[lane];

        foreach(Note n in notes)
        {
            if (beat >= n.beat && beat <= n.beat + blockDur && col == n.lane)
            {
                Debug.LogWarning("Spawning a note in a blocked segment: beat "
                + beat + " when blocked til " + col.blockedTil);

                return false;
            }

            if (n.beat == beat && n.lane == col)
            {
                Debug.LogWarning("Lane " + lane + " occupied by another note on same frame");
                return false;
            }
        }

        foreach (Note n in notes)
        {

        }

        if (!col.StreamOn)
        {
            Debug.LogWarning("Lane " + lane + " does not accept notes");
            return false;
        }

        return true;
    }

    public float getCurrBeat() { return currBeat; }

    public void broadCastHitAcc(float delta)
    {
        Transform canv = transform.Find("Canvas");
        GameObject popup = Instantiate(accuracyPopupPrefab, canv);
        popup.transform.position = accuracyPopupLoc.position;

        if (delta < perfectWindow)
        {
            // Perfect hit
            popup.GetComponent<Text>().text = "Perfect!";
        }
        else
        {
            // Regular hit
            popup.GetComponent<Text>().text = "OK!";
        }

    }
}