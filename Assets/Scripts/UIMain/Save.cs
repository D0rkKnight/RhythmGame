﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;

public class Save
{
    public string name;
    public bool[] nodes;
    public int stage = 0;
    public int tokens = 0;
    public float heat = 0;
    public string editorMap;

    public List<Ledger> highscores;
    public KeyCode[] keybinds;

    public Save(string name_)
    {
        name = name_;

        nodes = new bool[(int)SkillTree.NODE.SENTINEL];
        keybinds = new KeyCode[(int)InputManager.BINDS.SENTINEL];
        highscores = new List<Ledger>();
    }

    public void writeToSave()
    {
        for (int i=0; i<nodes.Length; i++)
            nodes[i] = SkillTree.sing.purchasedFlags[i];

        if (Timeliner.sing != null)
            stage = Timeliner.sing.Stage;
        tokens = ((MainSkillTree)SkillTree.sing).Tokens;
        heat = HeatController.sing.Heat;

        if (MapEditor.sing != null)
        {
            editorMap = MapEditor.sing.importField.text;
        }

        highscores.Clear();
        foreach (Ledger l in GameManager.sing.highscores)
        {
            Ledger nl = new Ledger(l.name, new List<int>(l.scores));
            highscores.Add(nl);
        }

        // Read keybinds
        // Cyclic if this is the active save
        // But lets us write data to other saves while referencing the active save
        for (int i = 0; i <= (int)InputManager.BINDS.COL_LAST - (int)InputManager.BINDS.COL_FIRST; i++)
        {
            keybinds[(int)InputManager.BINDS.COL_FIRST + i] = GameManager.getColKey(i);
        }
        keybinds[(int)InputManager.BINDS.PAUSE] = MusicPlayer.sing.pauseKey;
    }

    public void readFromSave()
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            if (i >= SkillTree.sing.purchasedFlags.Length)
                break;

            SkillTree.sing.purchasedFlags[i] = nodes[i];
        }

        if (Timeliner.sing != null)
            Timeliner.sing.Stage = stage;
        ((MainSkillTree)SkillTree.sing).Tokens = tokens;

        SkillTree.sing.compile(); // Just some weird switching that's necessary
        HeatController.sing.Heat = heat;
        SkillTree.sing.compile();

        if (MapEditor.sing != null)
        {
            MapEditor.sing.importField.text = editorMap;
        }

        GameManager.sing.highscores.Clear();
        foreach (Ledger l in highscores)
        {
            Ledger nl = new Ledger(l.name, new List<int>(l.scores));
            GameManager.sing.highscores.Add(nl);
        }

        readInputsFromSave();
    }
    public void readInputsFromSave()
    {
        // Write keybinds
        for (int i = 0; i <= (int)InputManager.BINDS.COL_LAST - (int)InputManager.BINDS.COL_FIRST; i++)
        {
            GameManager.setColKey(i, keybinds[(int)InputManager.BINDS.COL_FIRST + i]);
        }
        MusicPlayer.sing.pauseKey = keybinds[(int)InputManager.BINDS.PAUSE];
    }

    public void writeToDisk()
    {
        string o = "";
        o += "stage: " + Timeliner.sing.Stage + "\n";
        o += "tokens: " + ((MainSkillTree)SkillTree.sing).Tokens + "\n";
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

        foreach (Ledger l in highscores)
        {
            o += l.name + "\n";

            foreach (int s in l.scores)
                o += s + "\n";

            o += "\n";
        }

        // Write keybinds
        o += "\n";
        o += "keybinds\n";
        foreach (KeyCode c in keybinds)
        {
            o += c.ToString() + "\n";
        }

        // Write to file
        string path = Application.streamingAssetsPath + "/Saves/" + name + ".txt";
        StreamWriter writer = new StreamWriter(path);

        writer.WriteLine(o);
        writer.Close();
    }

    public static Save readFromDisk(string fname)
    {
        string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", fname + ".txt");
        StreamReader reader = new StreamReader(fpath);
        string data = reader.ReadToEnd();
        reader.Close();

        Save save = new Save(fname);

        string[] tokens = data.Split('\n');
        int block = 0; // Block 0 is the header
        int _node = 0;
        int _key = 0;

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
                            save.stage = int.Parse(toks[1]);
                            break;
                        case "tokens":
                            save.tokens = int.Parse(toks[1]);
                            break;
                        case "heat":
                            save.heat = int.Parse(toks[1]);
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
                    { 
                        // Check bounds
                        bool succ = int.TryParse(line, out int val);

                        // If can't parse, just skip
                        if (succ)
                        {
                            save.nodes[_node] = val > 0;
                            _node++;
                        }
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
                                save.editorMap = toks[1];
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

                    if (line.Equals("keybinds"))
                    {
                        block++;
                        break; // Short circuit and don't add a ledger
                    }

                    // Read in a block at a time
                    string name = line;
                    List<int> scores = new List<int>();

                    // Sample the next line
                    while (int.TryParse(tokens[i + 1].Trim(), out int res))
                    {
                        scores.Add(res);

                        i++;
                    }

                    save.highscores.Add(new Ledger(name, scores));
                    break;
                case 4:
                    if (_key < (int) InputManager.BINDS.SENTINEL)
                    {
                        object bObj = Enum.Parse(typeof(KeyCode), line);
                        save.keybinds[_key] = (KeyCode) bObj;

                        _key++;
                    }

                    break;
                default:
                    Debug.LogError("Block overflow");
                    break;
            }
        }

        return save;
    }

    public static bool exists(string name)
    {
        string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", name + ".txt");
        return File.Exists(fpath);
    }

    // Will overwrite any existing keys of the value
    public void setKey(InputManager.BINDS bind, KeyCode key)
    {
        for(int i=0; i<keybinds.Length; i++)
        {
            if (keybinds[i] == key)
                keybinds[i] = KeyCode.None;
        }

        keybinds[(int)bind] = key;
    }
}
