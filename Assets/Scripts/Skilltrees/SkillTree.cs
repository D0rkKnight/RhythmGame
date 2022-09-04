using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SkillTree : MonoBehaviour
{
    public enum NODE
    {
        L_EXPAND, R_EXPAND, ACCENT_1, ACCENT_2, ACCENT_3, HOLD, L_REROUTE, R_REROUTE, QUANT_1, QUANT_2, QUANT_3, // 0-10
        CORE_L, CORE_R, SKILLTREE, AUD_VIZ, SCORE, COMBO, HEAT, REBOUND, // 11-18
        L_DENSITY, R_DENSITY, // 19-20
        EIGHTH_NOTES, SIXTEENTH_NOTES, FREE_BEAT_GRAN, // 21-23
        SENTINEL,
    }

    public bool[] purchasedFlags = new bool[(int)NODE.SENTINEL];
    public bool[] activeFlags = new bool[(int)NODE.SENTINEL];
    public bool[] toggleFlags = new bool[(int)NODE.SENTINEL]; // Set by the music player at runtime
    public static SkillTree sing;

    public bool activateAll;
    public bool purchaseAll;

    public bool recompileQueued = false;

    // separate initialize function
    public virtual void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        init();

        if (purchaseAll) for (int i = 0; i < purchasedFlags.Length - 1; i++) purchasedFlags[i] = true;

        resetToggles();
    }

    public virtual void Start()
    {

    }

    public virtual void init()
    {

    }
    protected virtual void setActiveFlags()
    {

    }

    protected void applyToggles()
    {
        for(int i=0; i<(int) NODE.SENTINEL; i++)
        {
            activeFlags[i] = activeFlags[i] && toggleFlags[i];
        }
    }

    public void resetToggles()
    {
        // All toggles are on by default
        for (int i = 0; i < toggleFlags.Length; i++) toggleFlags[i] = true;
    }

    protected virtual void enableNewOptions()
    {

    }

    protected virtual void Update()
    {
        if (recompileQueued)
        {
            compile();
            recompileQueued = false;
        }
    }

    // Recompiles the game state depending on the given skill flags
    public void compile()
    {
        setActiveFlags(); // UI is set here
        applyToggles();

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
        mp.columns[0].reroute = activeFlags[(int)NODE.L_REROUTE] && !activeFlags[(int)NODE.L_EXPAND] ? mp.columns[1] : null;
        mp.columns[3].reroute = activeFlags[(int)NODE.R_REROUTE] && !activeFlags[(int)NODE.R_EXPAND] ? mp.columns[2] : null;

        if (activeFlags[(int)NODE.ACCENT_1]) ns.accentLim++;
        if (activeFlags[(int)NODE.ACCENT_2]) ns.accentLim++;

        ns.genType[(int)Phrase.TYPE.HOLD] = activeFlags[(int)NODE.HOLD];
        ns.genType[(int)Phrase.TYPE.REBOUND] = activeFlags[(int)NODE.REBOUND];

        ns.noteBlockLen = 0.5f;
        if (activeFlags[(int)NODE.QUANT_1]) ns.noteBlockLen = 0.25f;
        if (activeFlags[(int)NODE.QUANT_2]) ns.noteBlockLen = 0.125f;
        if (activeFlags[(int)NODE.QUANT_3]) ns.noteBlockLen = 0f;

        ns.beatGran = 0.25f;
        if (getActive(NODE.EIGHTH_NOTES)) ns.beatGran = 0.125f;
        if (getActive(NODE.SIXTEENTH_NOTES)) ns.beatGran = 0.0625f;
        if (getActive(NODE.FREE_BEAT_GRAN)) ns.beatGran = 0f;

        mp.columns[0].latCrowd = !getActive(NODE.L_DENSITY);
        mp.columns[1].latCrowd = !getActive(NODE.L_DENSITY);
        mp.columns[2].latCrowd = !getActive(NODE.R_DENSITY);
        mp.columns[3].latCrowd = !getActive(NODE.R_DENSITY);
    }

    public bool getActive(NODE n)
    {
        return activeFlags[(int)n];
    }

    public bool getPurchased(NODE n)
    {
        return purchasedFlags[(int)n];
    }
}
