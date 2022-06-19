using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timeliner : MonoBehaviour
{

    public GameObject cloak;
    private Animator anim;

    // Config
    public bool cloakedStart = true;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        string activeProfile = "main";

        if (cloakedStart)
        {
            cloak.SetActive(true); // Turn on cloak to begin with

            activeProfile = "new";
        }

        // Boot up skilltree and musicplayer
        SkillTree.sing.compile();
        SkillTree.sing.loadSave(activeProfile);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
