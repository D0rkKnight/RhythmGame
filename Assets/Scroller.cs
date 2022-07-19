using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scroller : MonoBehaviour, Clickable
{
    public MonoBehaviour target;
    public bool blocksInput = false;

    private void Awake()
    {
        if (!target is Scrollable)
            throw new System.Exception("Target not scrollable");
    }

    public int onClick(int code)
    {
        return blocksInput ? 1 : 0;
    }

    public int onOver()
    {
        if (Input.mouseScrollDelta.y != 0)
            ((Scrollable) target).ScrollBy(Input.mouseScrollDelta.y);

        return blocksInput ? 1 : 0;
    }
}
