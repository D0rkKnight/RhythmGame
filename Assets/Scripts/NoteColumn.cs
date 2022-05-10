using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteColumn : MonoBehaviour
{
    public float highlight = 0;
    public float decaySpeed = 4;
    Material highlightGrad;
    Material buttonOverlay;

    ParticleSystem burstSys;

    // Start is called before the first frame update
    void Awake()
    {
        highlightGrad = transform.Find("HitLight").
            GetComponent<SpriteRenderer>().material;
        buttonOverlay = transform.Find("TriggerBox/TBHitOverlay").
            GetComponent<SpriteRenderer>().material;
        burstSys = transform.Find("HitBurst").GetComponent<ParticleSystem>();
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
}
