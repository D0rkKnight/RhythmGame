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
    public string activeSave = "";

    public List<Ledger> highscores = new List<Ledger>();
    public int maxHighscores = 5;

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

        sing.activeSave = name;

        return data;
    }

    public static void loadSave(string data)
    {
        int _stage = 0; // Tutorial stage by default
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

        // Recompile
        SkillTree.sing.compile();
    }

    public static void writeSave(string name)
    {
        string o = "";
        o += "stage: " + Timeliner.sing.Stage + "\n";

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
        writeSave(sing.activeSave);
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

        led.scores.Add(score);
        led.scores.Sort();

        if (led.scores.Count > maxHighscores)
            led.scores.RemoveAt(0);
    }
}
