using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    public float hitTime; // In seconds, as Unity standard
    public float beat = 0; // Beat for hit
    public float blockDur = 0; // How long this note blocks other notes during rasterization
    public float weight = 0; // Priority when rasterizing

    public NoteColumn col;
    public bool dead = false;
    private bool showWeight = false;
    public bool ShowWeight
    {
        get { return showWeight;  }
        set
        {
            showWeight = value;
            text.text = value ? "" + weight : "";
        }
    }

    public Color defaultColor = Color.cyan;
    public Color deadColor = Color.grey;

    public bool hittable = true;
    public bool crossed = false; // Tracks whether the note has crossed the hitline

    public float Opacity
    {
        get { return noteBody.color.a; }
        set
        {
            defaultColor.a = value;
        }
    }

    public TMPro.TMP_Text text;

    private float killTime = -1;
    public float killDelay = 1;
    public float lerpSpeed = 3;
    public float lerpDist = 0.7f;
    private Vector2 killLerpTo = Vector2.zero;

    public SpriteRenderer noteBody;
    public SpriteRenderer highlightRend;
    public NoteClick clickListener;

    public Phrase phrase = null;

    public void Start()
    {
        clickListener.parent = this;
    }

    public void Update()
    {
        if (!dead) noteBody.color = defaultColor;
        else noteBody.color = deadColor;

        // Kill timer
        if (killTime > 0) {
            if (Time.time > killTime) Destroy(gameObject);

            // Lerp upwards
            transform.position = Vector3.Lerp(transform.position, killLerpTo, Time.deltaTime * lerpSpeed);

            // rend.color = Color.white;

            float intensity = (killTime - Time.time) / killDelay;
            intensity *= intensity * intensity;
            intensity -= 1.2f;
            noteBody.material.SetFloat("_Intensity", intensity);
        }
    }

    public virtual void setColor(Color c)
    {
        noteBody.color = c;
    }

    public void remove()
    {
        // Straight up destroy
        Destroy(gameObject);
    }

    public virtual void hit(out bool remove)
    {
        remove = true;
    }

    public virtual void kill()
    {
        // Lock to column?
        transform.position = col.gameObject.transform.position;

        // Is released from the music player already
        killTime = Time.time + killDelay;
        killLerpTo = transform.position + Vector3.up * lerpDist;

        transform.Find("Trail").gameObject.SetActive(false); // Deactivate trail
    }


    public virtual void updateNote(MusicPlayer mp, List<Note> passed)
    {
        Vector2 tPos = col.transform.Find("TriggerBox").position;

        float dt = getHitTime() - mp.songTime;

        Vector2 dp = -mp.dir * dt * mp.travelSpeed;
        Vector2 p = tPos + dp;
        transform.position = new Vector3(p.x, p.y, -1);

        // Check if strictly unhittable (also can't miss a dead note no matter what)
        if (!dead && checkMiss(mp, dt))
        {
            mp.miss(this);
        }

        // Sort into discard pile (separate process from missing)
        if (passed != null)
        {
            float noteExtension = getNoteExtension(mp);

            if (dt < -(mp.noteTimeout + noteExtension)) passed.Add(this);
        }

        if (dt < 0 && !crossed)
        {
            crossed = true;
            onCross();
        }
    }

    public virtual bool checkMiss(MusicPlayer mp, float dt)
    {
        return dt < -mp.hitWindow;
    }

    protected virtual float getNoteExtension(MusicPlayer mp)
    {
        return 0;
    }

    public virtual void resetInit(MusicPlayer mp)
    {
        dead = false;
    }

    public virtual void onBeat(MusicPlayer mp)
    {

    }

    public virtual void onLaneRelease()
    {

    }

    public virtual void onScroll(MusicPlayer mp)
    {

    }

    public virtual float getHitTime()
    {
        return hitTime;
    }

    public virtual void blocked()
    {

    }

    public virtual void childBlocked(Note child)
    {

    }

    public virtual void onCross()
    {

    }
}
