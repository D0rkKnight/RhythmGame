using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    public float hitTime; // In seconds, as Unity standard
    public float beat = 0; // Beat for hit
    public MusicPlayer.Column lane;
    public bool dead = false;

    public Color defaultColor = Color.cyan;
    public Color deadColor = Color.grey;

    private float killTime = -1;
    public float killDelay = 1;
    public float lerpSpeed = 3;
    public float lerpDist = 0.7f;
    private Vector2 killLerpTo = Vector2.zero;

    private SpriteRenderer rend;
    private void Awake()
    {
        rend = GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        if (!dead) rend.color = defaultColor;
        else rend.color = deadColor;

        // Kill timer
        if (killTime > 0) {
            if (Time.time > killTime) Destroy(gameObject);

            // Lerp upwards
            transform.position = Vector3.Lerp(transform.position, killLerpTo, Time.deltaTime * lerpSpeed);

            // rend.color = Color.white;

            float intensity = (killTime - Time.time) / killDelay;
            intensity *= intensity * intensity;
            intensity -= 1.2f;
            rend.material.SetFloat("_Intensity", intensity);
        }
    }

    public virtual void highlight(Color c)
    {
        GetComponent<SpriteRenderer>().color = c;
    }

    public void remove()
    {
        // Straight up destroy
        Destroy(gameObject);
    }

    public void hit()
    {
        // Lock to column?
        transform.position = lane.gObj.transform.position;

        // Is released from the music player already
        killTime = Time.time + killDelay;
        killLerpTo = transform.position + Vector3.up * lerpDist;

        transform.Find("Trail").gameObject.SetActive(false); // Deactivate trail
    }
}
