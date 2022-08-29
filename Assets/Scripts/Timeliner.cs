using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Timeliner : MonoBehaviour
{
    public GameObject cloak;
    private Animator anim;

    public bool mpCentered = false;
    public Transform mpCenteredMarker;
    public Transform mpOffsetMarker;
    public float mpLerpSpeed = 4f;

    public Transform stCenteredMarker;
    public Transform stOffsetMarker;

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
        string activeProfile = GameManager.saveToLoad;
        string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", activeProfile + ".txt");

        if (!GameManager.saveExists(activeProfile))
        {
            // Copy main.txt
            string mainPath = Path.Combine(Application.streamingAssetsPath, "Saves", "main.txt");
            File.Copy(mainPath, fpath);
        }

        GameManager.activeSave = Save.readFromDisk(GameManager.saveToLoad);
        GameManager.activeSave.readFromSave();

        // Boot up skilltree and musicplayer
        SkillTree.sing.compile();
    }

    // Update is called once per frame
    void Update()
    {

        // State animator for music player
        Transform mpLerpMarker = mpCentered ? mpCenteredMarker : mpOffsetMarker;
        MusicPlayer.sing.transform.position = Vector3.Lerp(
            MusicPlayer.sing.transform.position, mpLerpMarker.transform.position, mpLerpSpeed * Time.deltaTime);

        Transform stLerpMarker = !mpCentered ? stCenteredMarker : stOffsetMarker;
        Vector3 newSTPos = Vector3.Lerp(
            SkillTree.sing.transform.position, stLerpMarker.transform.position, mpLerpSpeed * Time.deltaTime);
        SkillTree.sing.transform.position = newSTPos;
    }
}
