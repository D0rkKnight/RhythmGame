using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillTree : MonoBehaviour
{
    public enum NODE
    {
        L_EXPAND, R_EXPAND, ACCENT_1, ACCENT_2, ACCENT_3, HOLD, L_REROUTE, R_REROUTE, QUANT_1, QUANT_2, QUANT_3,
        SENTINEL
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

        MusicPlayer mp = MusicPlayer.sing;
        MapSerializer ns = MapSerializer.sing;

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

        enableNewOptions();
    }
}
