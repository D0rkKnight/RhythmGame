using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timeliner : MonoBehaviour
{

    public GameObject cloak;
    private Animator anim;

    // Config
    public bool newStart = true;
    public string forceProfile = "";
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

    public string[] mapList;
    public List<string> mapQueue = new List<string>();

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
        string activeProfile = "main";

        if (newStart)
        {
            cloak.SetActive(true); // Turn on cloak to begin with

            activeProfile = "new";
        }

        if (forceProfile.Length > 0)
            activeProfile = forceProfile;

        string saveData = GameManager.getSave(activeProfile);

        // Boot up skilltree and musicplayer
        SkillTree.sing.compile();
        GameManager.loadSave(saveData);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void playNextMap()
    {
        if (mapQueue.Count == 0)
        {
            // Regenerate queue
            foreach (string s in mapList)
                mapQueue.Add(s);

            // Shuffle
            for (int i=0; i<mapQueue.Count; i++)
            {
                // Choose a random spot to swap to
                int nextInd = Random.Range(0, mapQueue.Count);

                string tmp = mapQueue[nextInd];
                mapQueue[nextInd] = mapQueue[i];
                mapQueue[i] = tmp;
            }
        }

        string nextMapName = mapQueue[0];
        mapQueue.RemoveAt(0);

        Debug.Log(nextMapName);

        MapSerializer.sing.playMap(nextMapName);
    }
}
