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

    private SpriteRenderer rend;
    private void Awake()
    {
        rend = GetComponent<SpriteRenderer>();
    }

    public void Update()
    {
        if (!dead) rend.color = defaultColor;
        else rend.color = deadColor;
    }

    public virtual void highlight(Color c)
    {
        GetComponent<SpriteRenderer>().color = c;
    }
}
