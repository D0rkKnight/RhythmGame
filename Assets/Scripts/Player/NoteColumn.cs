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
    public GameObject charTxt;
    public int colNum;

    public KeyCode Key
    {
        get { return GameManager.sing.colKeys[colNum];  }
        set
        {
            GameManager.sing.colKeys[colNum] = value;

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
            transform.Find("ColumnBG").gameObject.SetActive(value);
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
}
