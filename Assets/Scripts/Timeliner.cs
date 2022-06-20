using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timeliner : MonoBehaviour
{

    public GameObject cloak;
    private Animator anim;

    // Config
    public bool newStart = true;
    private int stage = 0;
    public int Stage
    {
        get { return stage; }
        set
        {
            stage = value;
            anim.SetInteger("stage", value);
        }
    }

    public static Timeliner sing;

    private void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        anim = GetComponent<Animator>();
        Stage = Stage; // Update anim
    }

    // Start is called before the first frame update
    void Start()
    {
        string activeProfile = "main.txt";

        if (newStart)
        {
            cloak.SetActive(true); // Turn on cloak to begin with

            activeProfile = "new.txt";
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
