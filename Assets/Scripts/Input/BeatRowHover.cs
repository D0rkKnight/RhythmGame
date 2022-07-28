using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatRowHover : MonoBehaviour, Clickable
{
    BeatRow par;

    public GameObject delBut;
    public GameObject halo;

    private void Start()
    {
        par = GetComponentInParent<BeatRow>();

        halo.SetActive(false);
        delBut.SetActive(false);
    }

    public int onClick(int code)
    {
        return 1;
    }

    public int onOver()
    {
        return 1; // Doesn't block
    }

    public void OnMouseEnter()
    {
        delBut.SetActive(true);
        halo.SetActive(true);
    }

    public void OnMouseExit()
    {
        delBut.SetActive(false);
        halo.SetActive(false);
    }
}
