using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class GameManager : MonoBehaviour
{

    public static GameManager sing;

    public GameObject settings;
    public KeyCode[] colKeys;
    public KeyCode optionsKey = KeyCode.Escape;

    public Stack<GameObject> panelStack = new Stack<GameObject>(); // Tracks the active stack of ui panels
    public GameObject activePanel = null;

    public string forceSave = "";
    public static string saveToLoad = "test";
    public static Save activeSave;

    public bool saveOnInter = true;

    public List<Ledger> highscores = new List<Ledger>();
    public int maxHighscores = 5;
    public bool wasHighscore = false;

    public string[] mapList;
    public List<string> mapQueue = new List<string>();

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Gamemanager Singleton broken (very bad)");
        sing = this;

        Phrase.init();
    }

    private void Start()
    {
        // Hide all ui panels
        foreach (Transform child in transform.Find("Canvas"))
        {
            if (child.tag == "UIPanel")
            {
                child.gameObject.SetActive(false);
            }
        }

        // Force a save load
        if (forceSave.Length > 0)
        {
            activeSave = Save.readFromDisk(forceSave);
            activeSave.readFromSave();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(optionsKey))
        {
            if (panelStack.Peek() == settings)
                popPanelStack();
            else
                openOptions();
        }
    }

    public void changeScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void pushPanelStack(GameObject obj, bool wipesLastPanel = true)
    {
        // Deactivate last element
        if (panelStack.Count > 0 && wipesLastPanel)
            panelStack.Peek().SetActive(false);

        // Activate new element
        panelStack.Push(obj);
        obj.SetActive(true);
    }

    public GameObject popPanelStack()
    {
        // Deactivate top element
        GameObject top = panelStack.Pop();
        if (top != null) top.SetActive(false);

        // Activate next element
        if (panelStack.Count > 0)
            panelStack.Peek().SetActive(true);

        return top;
    }

    public void addScore(string songname, int score)
    {
        // Find entry in ledger
        Ledger led = highscores.Find( 
            (Ledger l) => { 
                return l.name.Equals(songname);  
            }
        );

        if (led == null)
        {
            // Create a new ledger if need be
            led = new Ledger(songname, new List<int>());
            highscores.Add(led);
        }

        // Check against last score since that should be highest
        // Or set to true if no prior scores
        wasHighscore = led.scores.Count == 0 ||
            score > led.scores[led.scores.Count - 1];

        led.scores.Add(score);
        led.scores.Sort();

        if (led.scores.Count > maxHighscores)
            led.scores.RemoveAt(0);
    }

    public void playNextMap()
    {
        // Doesn't use the map queue if map editor is active
        if (MapEditor.sing != null && MapEditor.sing.isActiveAndEnabled)
        {
            MapEditor.sing.play();
            return;
        }
            

        if (mapQueue.Count == 0)
        {
            // Regenerate queue
            foreach (string s in mapList)
                mapQueue.Add(s);

            // Shuffle
            for (int i = 0; i < mapQueue.Count; i++)
            {
                // Choose a random spot to swap to
                int nextInd = UnityEngine.Random.Range(0, mapQueue.Count);

                string tmp = mapQueue[nextInd];
                mapQueue[nextInd] = mapQueue[i];
                mapQueue[i] = tmp;
            }
        }

        string nextMapName = mapQueue[0];
        mapQueue.RemoveAt(0);

        MapSerializer.sing.playMap(nextMapName);
    }

    public static void writeSave()
    {
        activeSave.writeToSave();
        activeSave.writeToDisk();
    }

    public void openOptions()
    {
        pushPanelStack(settings);
        if (MusicPlayer.sing != null)
            MusicPlayer.sing.pause();
    }
}
