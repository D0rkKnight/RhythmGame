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
        L_EXPAND, R_EXPAND, ACCENT_1, ACCENT_2, ACCENT_3, HOLD, IN_BTWN, SENTINEL
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

    // Start is called before the first frame update
    void Start()
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

        mp.columns[0].Active = flags[(int) NODE.L_EXPAND];
        mp.columns[3].Active = flags[(int) NODE.R_EXPAND];

        if (flags[(int)NODE.ACCENT_1]) ns.accentLim++;
        if (flags[(int)NODE.HOLD]) ns.genHolds = true;



        // Activate new skills
        foreach (buttonPair bp in nodes)
        {
            if (!bp.btn.gameObject.active)
                bp.checkPrereqs();
        }
    }
}
