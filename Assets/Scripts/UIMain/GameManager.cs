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

    public Stack<GameObject> panelStack = new Stack<GameObject>(); // Tracks the active stack of ui panels
    public GameObject activePanel = null;

    public string forceSave = "";

    [SerializeField] 
    public static string activeSave = "";

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
            loadSave(getSave(forceSave));
    }

    // Update is called once per frame
    void Update()
    {
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

    public static string getSave(string name)
    {
        string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", name + ".txt");
        StreamReader reader = new StreamReader(fpath);
        string data = reader.ReadToEnd();
        reader.Close();

        activeSave = name;

        return data;
    }

    public static void loadSave(string data)
    {
        int _stage = 0; // Tutorial stage by default
        int _tokens = 0;
        int _heat = 0;
        string[] tokens = data.Split('\n');

        int block = 0; // Block 0 is the header
        int _node = 0;

        // Parse tokens (start with header)
        for (int i = 0; i < tokens.Length; i++)
        {
            string line = tokens[i].Trim();

            // colon delimit
            string[] toks = line.Split(":");
            for (int j = 0; j < toks.Length; j++)
            {
                toks[j] = toks[j].Trim();
            }

            if (line.Length == 0) continue; // Skip empty lines

            switch (block)
            {
                case 0:
                    switch (toks[0])
                    {
                        case "stage":
                            _stage = int.Parse(toks[1]);
                            break;
                        case "tokens":
                            _tokens = int.Parse(toks[1]);
                            break;
                        case "heat":
                            _heat = int.Parse(toks[1]);
                            break;
                        case "nodes":
                            block++;
                            break;
                        default:
                            Debug.LogError("Illegal header token");
                            break;
                    }

                    break;
                case 1:
                    // Reading nodes
                    if (_node < (int)SkillTree.NODE.SENTINEL)
                    { // Check bounds

                        SkillTree.sing.purchasedFlags[_node] = int.Parse(line) > 0;
                        _node++;
                    }

                    if (line.Equals("editor"))
                        block++;

                    break;
                case 2:
                    switch (toks[0])
                    {
                        case "active":
                            if (MapEditor.sing != null && toks.Length >= 2)
                            {
                                Debug.Log("Preloaded " + toks[1]);

                                MapEditor.sing.importField.text = toks[1];
                            }

                            break;
                        case "highscores":
                            block++;
                            break;
                        default:
                            Debug.LogError("Illegal header token");
                            break;
                    }

                    break;
                case 3:

                    // Read in a block at a time
                    string name = line;
                    List<int> scores = new List<int>();

                    // Sample the next line
                    while (int.TryParse(tokens[i+1].Trim(), out int res))
                    {
                        scores.Add(res);

                        i++;
                    }

                    sing.highscores.Add(new Ledger(name, scores));
                    break;
                default:
                    Debug.LogError("Block overflow");
                    break;
            }
        }

        if (Timeliner.sing != null)
        {
            Timeliner.sing.Stage = _stage;
        }

        // Maybe delegate this load action to the skilltree class
        if (SkillTree.sing is MainSkillTree)
            ((MainSkillTree)SkillTree.sing).Tokens = _tokens;

        // Recompile (activates heat mechanic)
        SkillTree.sing.compile();

        HeatController.sing.Heat = _heat;

        // Recompile again (enables heat gated skills)
        SkillTree.sing.compile();
    }

    public static void writeSave(string name)
    {
        string o = "";
        o += "stage: " + Timeliner.sing.Stage + "\n";
        o += "tokens: " + ((MainSkillTree) SkillTree.sing).Tokens + "\n";
        o += "heat: " + HeatController.sing.Heat + "\n";

        o += "\n";
        o += "nodes\n";

        foreach (bool b in SkillTree.sing.purchasedFlags)
            o += b ? "1\n" : "0\n";

        o += "\n";
        o += "editor\n";
        o += "active: \n"; // Doesn't write in an active file

        o += "\n";
        o += "highscores\n";

        foreach (Ledger l in sing.highscores)
        {
            o += l.name + "\n";

            foreach (int s in l.scores)
                o += s + "\n";

            o += "\n";
        }

        // Write to file
        string path = Application.streamingAssetsPath + "/Saves/" + name + ".txt";
        StreamWriter writer = new StreamWriter(path);

        writer.WriteLine(o);
        writer.Close();
    }

    public static void writeSave()
    {
        writeSave(activeSave);
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
}
