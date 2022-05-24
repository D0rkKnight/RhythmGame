using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

public class SkillTree : MonoBehaviour
{
    public enum NODE
    {
        L_EXPAND, R_EXPAND, ACCENT_1, ACCENT_2, ACCENT_3, HOLD, L_REROUTE, R_REROUTE, QUANT_1, QUANT_2, QUANT_3,
        SENTINEL
    }

    [System.Serializable]
    public class buttonPair
    {
        public NODE node;
        public Button btn;
        public NODE[] prereqs;

        private SkillTree owner;

        public buttonPair(NODE node_, Button btn_)
        {
            node = node_;
            btn = btn_;
        }

        public void init(SkillTree owner_)
        {
            this.owner = owner_;
            btn.onClick.AddListener(onClick);

            checkPrereqs();
        }

        private void onClick()
        {
            owner.flags[(int)node] = true;
            owner.compile();
        }

        public void checkPrereqs()
        {
            bool fulfilled = true;
            foreach (NODE n in prereqs)
            {
                if (!owner.flags[(int)n])
                {
                    fulfilled = false;
                    break;
                }
            }
            btn.gameObject.SetActive(fulfilled);
        }
    }

    public buttonPair[] nodes = new buttonPair[(int) NODE.SENTINEL];
    public bool[] flags = new bool[(int)NODE.SENTINEL];
    public static SkillTree sing;

    // separate initialize function
    void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        for (int i=0; i<nodes.Length; i++)
        {
            nodes[i].init(this);
        }
    }

    // Recompiles the game state depending on the given skill flags
    public void compile()
    {
        MusicPlayer mp = MusicPlayer.sing;
        MapSerializer ns = MapSerializer.sing;

        mp.columns[0].StreamOn = flags[(int)NODE.L_EXPAND];
        mp.columns[3].StreamOn = flags[(int)NODE.R_EXPAND];

        // Whether to reroute input on the two columns
        mp.columns[0].reroute = flags[(int)NODE.L_EXPAND] ? null : mp.columns[1];
        mp.columns[3].reroute = flags[(int)NODE.R_EXPAND] ? null : mp.columns[2];

        if (flags[(int)NODE.ACCENT_1]) ns.accentLim++;
        if (flags[(int)NODE.HOLD]) ns.genType[(int) Phrase.TYPE.HOLD] = true;


        // Activate new skills
        foreach (buttonPair bp in nodes)
        {
            if (!bp.btn.gameObject.activeSelf)
                bp.checkPrereqs();
        }
    }
}
