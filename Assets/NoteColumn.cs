using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteColumn : MonoBehaviour
{
    public float highlight = 0;
    public float decaySpeed = 4;
    Material highlightGrad;

    // Start is called before the first frame update
    void Awake()
    {
        highlightGrad = transform.Find("HitLight").
            GetComponent<SpriteRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        highlight = Mathf.Clamp(highlight, 0, 1); // Keep between 0 and 1 ok
        highlightGrad.SetFloat("_Intensity", highlight);

        // Lerp highlight back to 0
        highlight = Mathf.Lerp(highlight, 0, Time.deltaTime * decaySpeed);
    }
}
