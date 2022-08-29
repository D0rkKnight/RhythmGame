using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NoteColumn : MonoBehaviour
{
    public float highlight = 0;
    public float decaySpeed = 4;
    Material highlightGrad;
    Material buttonOverlay;

    ParticleSystem burstSys;
    public GameObject bg;

    public GameObject charTxt;
    public int colNum;

    public Transform beatLinePrefab;
    public List<Transform> beatLines = new List<Transform>();
    public bool showBeat = false;
    public bool latCrowd = false;
    public KeyCode Key
    {
        get { return GameManager.getColKey(colNum);  }
        set
        {
            GameManager.setColKey(colNum, value);

            charTxt.GetComponent<TextMeshProUGUI>().SetText(value.ToString());
        }
    }
    public NoteColumn reroute;  // Whether to substitute input for other columns

    public int defNoteReroute;     // Default column to reroute notes to
    private bool active = true;
    public bool Active
    {
        get { return active; }
        set
        {
            active = value;
            gameObject.SetActive(value);
        }
    }

    private bool streamOn = true;
    public bool StreamOn
    {
        get { return streamOn; }
        set
        {
            streamOn = value;

            // Assign value to background and lights
            bg.gameObject.SetActive(value);
            transform.Find("HitLight").gameObject.SetActive(value);
            transform.Find("HitBurst").gameObject.SetActive(value);
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        highlightGrad = transform.Find("HitLight").
            GetComponent<SpriteRenderer>().material;
        buttonOverlay = transform.Find("TriggerBox/TBHitOverlay").
            GetComponent<SpriteRenderer>().material;
        burstSys = transform.Find("HitBurst").GetComponent<ParticleSystem>();
        
    }

    private void Start()
    {
        Key = Key; // Update graphics

        // Only do beatlines in the editor
        if (MapEditor.sing != null)
        {
            // Load in beat lines
            float stackHeight = bg.transform.localScale.y;
            float stackDist = MusicPlayer.sing.beatInterval * MusicPlayer.sing.travelSpeed;

            for (float alt = 0; alt <= stackHeight; alt += stackDist)
            {
                Transform line = Instantiate(beatLinePrefab, transform);
                beatLines.Add(line);

                line.Find("Canvas/BeatMarker").gameObject.SetActive(showBeat);
            }
            updateLines();
        }
    }

    // Update is called once per frame
    void Update()
    {
        highlight = Mathf.Clamp(highlight, 0, 1); // Keep between 0 and 1 ok
        highlightGrad.SetFloat("_Intensity", highlight);

        float bOverInt = highlight * 2;
        bOverInt = ((float) Mathf.Pow(2.5f, bOverInt) - 1) / 2.5f;
        buttonOverlay.SetFloat("_Intensity", bOverInt);

        // Decrease highlight back to 0
        // highlight = Mathf.Lerp(highlight, 0, Time.deltaTime * decaySpeed);
        highlight = Mathf.Max(0, highlight - Time.deltaTime * decaySpeed);

        updateLines();
    }

    public void hitBurst()
    {
        burstSys.Play();
    }

    public static void Regenerate()
    {
        // Proc the delegate behavior
        foreach (NoteColumn col in MusicPlayer.sing.columns)
            col.Key = col.Key;
    }

    public void updateLines()
    {
        MusicPlayer mp = MusicPlayer.sing;
        float stackHeight = bg.transform.localScale.y;
        float distPerBeat = mp.beatInterval * mp.travelSpeed;

        float minBeat = Mathf.Ceil( mp.getCurrBeat());

        for (int i = 0; i < beatLines.Count; i++)
        {

            Transform line = beatLines[i];
            line.gameObject.SetActive(true);

            // Make sure this value is valid
            if (mp.beatInterval == 0 || !float.IsFinite(mp.beatInterval))
            {
                line.gameObject.SetActive(false);
                continue;
            }

                float beatOn = minBeat + i;
            float alt = beatOn * distPerBeat - (mp.getCurrBeat() * mp.beatInterval * mp.travelSpeed);

            Vector2 dir = mp.dir;
            Vector2 lPos = -dir * alt;

            Vector2 perp = new Vector2(dir.y, -dir.x);
            lPos -= perp * 0.5f;

            line.localPosition = new Vector3(lPos.x, lPos.y, 0);

            Transform lineR = line.Find("Line");
            Vector3 lineRScale = lineR.localScale;
            lineRScale.x = transform.localScale.x;
            lineR.localScale = lineRScale;

            line.Find("Canvas/BeatMarker").GetComponent<TMPro.TMP_Text>().text = "" + beatOn;
        }
    }

    public float getMBeatRounded()
    {
        // Check up and down drag
        MusicPlayer mp = MusicPlayer.sing;

        Vector2 delta = (Camera.main.ScreenToWorldPoint(Input.mousePosition) -
            transform.Find("TriggerBox").position);
        float dist = Vector2.Dot(delta, -mp.dir);

        float mBeat = mp.getCurrBeat() + (dist / mp.travelSpeed / mp.beatInterval);

        // Round to quarter beat
        float rBeat = Mathf.Round(mBeat * 4) / 4;

        return rBeat;
    }
}
