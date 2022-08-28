using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

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
        string activeProfile = GameManager.activeSave;
        string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", activeProfile + ".txt");

        if (!File.Exists(fpath))
        {
            // Copy main.txt
            string mainPath = Path.Combine(Application.streamingAssetsPath, "Saves", "main.txt");
            File.Copy(mainPath, fpath);
        }

        string saveData = GameManager.getSave(activeProfile);

        // Boot up skilltree and musicplayer
        SkillTree.sing.compile();
        GameManager.loadSave(saveData);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
