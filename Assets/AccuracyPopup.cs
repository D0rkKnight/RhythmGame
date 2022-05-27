using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AccuracyPopup : MonoBehaviour
{

    public float deathTime;
    public float life = 1f; // Total lifetime in seconds
    public float apex = 0.4f; // Distance to popin apex;

    // Start is called before the first frame update
    void Start()
    {
        deathTime = Time.time + life;
        transform.localScale = Vector3.one * 0.01f; // Start very small
    }

    // Update is called once per frame
    void Update()
    {
        float t = life - (deathTime - Time.time); // Grows

        if (t < apex)
        {
            // Lerp towards max size
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 40);
        } else
        {
            // Decay to zero linearly
            transform.localScale = Vector3.Max(transform.localScale - Vector3.one * Time.deltaTime * 0.5f, Vector3.zero);

            // Ramp alpha too
            Text txt = GetComponent<Text>();
            txt.color = new Vector4(txt.color.r, txt.color.g, txt.color.b, 1-(t-apex)/(life-apex));
        }

        if (t > life) Destroy(gameObject);
    }
}
