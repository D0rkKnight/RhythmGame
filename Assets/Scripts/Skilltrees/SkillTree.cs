using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SkillTree : MonoBehaviour
{
    public enum NODE
    {
        L_EXPAND, R_EXPAND, ACCENT_1, ACCENT_2, ACCENT_3, HOLD, L_REROUTE, R_REROUTE, QUANT_1, QUANT_2, QUANT_3,
        CORE_L, CORE_R, SKILLTREE, AUD_VIZ, SCORE, COMBO, HEAT, SENTINEL,
    }

    public bool[] purchasedFlags = new bool[(int)NODE.SENTINEL];
    public bool[] activeFlags = new bool[(int)NODE.SENTINEL];
    public static SkillTree sing;

    public bool activateAll;
    public bool purchaseAll;

    // separate initialize function
    public virtual void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        init();

        if (purchaseAll) for (int i = 0; i < purchasedFlags.Length - 1; i++) purchasedFlags[i] = true;
    }

    public virtual void init()
    {

    }
    protected virtual void setActiveFlags()
    {

    }
    protected virtual void enableNewOptions()
    {

    }

    // Recompiles the game state depending on the given skill flags
    public void compile()
    {
        setActiveFlags();

        compileMech();

        enableNewOptions();
    }

    protected virtual void compileMech()
    {
        MusicPlayer mp = MusicPlayer.sing;
        MapSerializer ns = MapSerializer.sing;

        // Set core elements
        mp.columns[1].Active = activeFlags[(int)NODE.CORE_L];
        mp.columns[2].Active = activeFlags[(int)NODE.CORE_R];
        mp.columns[1].StreamOn = activeFlags[(int)NODE.CORE_L];
        mp.columns[2].StreamOn = activeFlags[(int)NODE.CORE_R];

        mp.columns[0].Active = activeFlags[(int)NODE.L_REROUTE];
        mp.columns[3].Active = activeFlags[(int)NODE.R_REROUTE];

        mp.columns[0].StreamOn = activeFlags[(int)NODE.L_EXPAND];
        mp.columns[3].StreamOn = activeFlags[(int)NODE.R_EXPAND];

        // Whether to reroute input on the two columns
        mp.columns[0].reroute = activeFlags[(int)NODE.L_REROUTE] ? mp.columns[1] : null;
        mp.columns[3].reroute = activeFlags[(int)NODE.R_REROUTE] ? mp.columns[2] : null;

        if (activeFlags[(int)NODE.ACCENT_1]) ns.accentLim++;
        if (activeFlags[(int)NODE.ACCENT_2]) ns.accentLim++;

        if (activeFlags[(int)NODE.HOLD]) ns.genType[(int)Phrase.TYPE.HOLD] = true;

        ns.noteBlockLen = 0.5f;
        if (activeFlags[(int)NODE.QUANT_1]) ns.noteBlockLen = 0.25f;
        if (activeFlags[(int)NODE.QUANT_2]) ns.noteBlockLen = 0.125f;
        if (activeFlags[(int)NODE.QUANT_3]) ns.noteBlockLen = 0f;
    }

    public void loadSave(string name)
    {
        string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", name);
        StreamReader reader = new StreamReader(fpath);
        string data = reader.ReadToEnd();

        int _stage = 0; // Tutorial stage by default
        string[] tokens = data.Split('\n');

        int block = 0; // Block 0 is the header
        int _node = 0;

        // Parse tokens (start with header)
        for (int i=0; i<tokens.Length; i++)
        {
            string line = tokens[i].Trim();

            if (line.Length == 0) continue; // Skip empty lines

            switch (block)
            {
                case 0:
                    // colon delimit
                    string[] toks = line.Split(":");
                    for (int j=0; j<toks.Length; j++)
                    {
                        toks[j] = toks[j].Trim();
                    }

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
                    if (_node >= (int)NODE.SENTINEL) break; // Out of bounds

                    purchasedFlags[_node] = int.Parse(line) > 0;
                    _node++;

                    break;
                default:
                    Debug.LogError("Block overflow");
                    break;
            }
        }

        Timeliner.sing.Stage = _stage;
        Debug.Log("Stage: "+_stage);

        // Recompile
        compile();
    }
}
