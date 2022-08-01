using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPlayScroller : MonoBehaviour, Clickable
{

    public float scrollSpeed = 0.25f;
    int Clickable.onClick(int code)
    {
        return 1;
    }

    int Clickable.onOver()
    {
        if (MusicPlayer.sing.state == MusicPlayer.STATE.PAUSE) 
            MusicPlayer.sing.scrollBy(Input.mouseScrollDelta.y*scrollSpeed);
        return 1;
    }
}
