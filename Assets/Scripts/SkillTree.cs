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
        public int cost = 100;
        public float heatReq = 0;

        private SkillTree owner;

        public buttonPair(NODE node_, Button btn_)
        {
            node = node_;
            btn = btn_;
        }

        public void init(SkillTree owner_)
        {
            owner = owner_;
            btn.onClick.AddListener(onClick);

            // Set label text
            btn.GetComponentInChildren<Text>().text += " $" + cost.ToString();

            checkPrereqs();
        }

        private void onClick()
        {
            if (owner.tokens < cost) return; // Not enough money :(
            owner.tokens -= cost;

            owner.purchasedFlags[(int)node] = true;
            owner.compile();
        }

        public void checkPrereqs()
        {
            bool fulfilled = true;
            foreach (NODE n in prereqs)
            {
                if (!owner.purchasedFlags[(int)n])
                {
                    fulfilled = false;
                    break;
                }
            }
            btn.gameObject.SetActive(fulfilled);
        }
    }

    public buttonPair[] nodes = new buttonPair[(int) NODE.SENTINEL];
    public bool[] purchasedFlags = new bool[(int)NODE.SENTINEL];
    public bool[] activeFlags = new bool[(int)NODE.SENTINEL];

    public static SkillTree sing;

    public Text tokenText;
    private int tokens = 0;
    public int Tokens
    {
        get { return tokens; }
        set
        {
            tokens = value;
            tokenText.text = "$"+value.ToString();
        }
    }

    private int subToken;
    public int pointsPerToken = 1000;
    public int SubToken
    {
        get { return subToken; }
        set
        {
            subToken = value;

            int newToks = Tokens;
            while (subToken > pointsPerToken)
            {
                subToken -= pointsPerToken;
                newToks += 1;
            }
            Tokens = newToks;
        }
    }

    public GameObject lineRendPrefab;
    public List<LineRenderer> lineRends;

    public bool activateAll;
    public bool purchaseAll;

    // separate initialize function
    void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        for (int i=0; i<nodes.Length; i++)
        {
            nodes[i].init(this);
        }

        if (purchaseAll) for (int i = 0; i < purchasedFlags.Length-1; i++) purchasedFlags[i] = true;

        lineRends = new List<LineRenderer>();
    }

    // Recompiles the game state depending on the given skill flags
    public void compile()
    {
        // Find active flags
        for (int i = 0; i < (int)NODE.SENTINEL - 1; i++)
        {
            buttonPair nodeData = nodes[i];
            int index = (int)nodeData.node;
            activeFlags[index] = nodeData.heatReq <= HeatController.sing.Heat || activateAll;

            // Set opacity of buttons
            float opacity = activeFlags[index] ? 1 : 0.5f;
            Image img = nodeData.btn.GetComponent<Image>();
            img.color = new Color(img.color.r, img.color.g, img.color.b, opacity);
        }

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

        if (activeFlags[(int)NODE.HOLD]) ns.genType[(int) Phrase.TYPE.HOLD] = true;

        ns.noteBlockLen = 0.5f;
        if (activeFlags[(int)NODE.QUANT_1]) ns.noteBlockLen = 0.25f;
        if (activeFlags[(int)NODE.QUANT_2]) ns.noteBlockLen = 0.125f;
        if (activeFlags[(int)NODE.QUANT_3]) ns.noteBlockLen = 0f;


        // Activate new skills
        foreach (buttonPair bp in nodes)
        {
            if (!bp.btn.gameObject.activeSelf)
                bp.checkPrereqs();
        }

        regenLines();
    }

    private void regenLines()
    {
        // Clear old lines
        foreach (LineRenderer lr in lineRends) Destroy(lr);
        lineRends.Clear();

        // Gen new lines
        foreach (buttonPair bp in nodes)
        {
            if (bp.btn.IsActive())
            {

                // Draw lines between active skills (from an active skill to its parent)
                // 4 points: base, turn 1, turn 2, end

                RectTransform bpBounds = bp.btn.GetComponent<RectTransform>();
                Vector3 bpBase = bpBounds.position;

                foreach (NODE prereq in bp.prereqs)
                {
                    // Find node to hook to
                    Button target = null;
                    foreach (buttonPair search in nodes) if (search.node == prereq)
                        {
                            target = search.btn;
                            break;
                        }

                    RectTransform endBounds = target.GetComponent<RectTransform>();
                    Vector3 bpEnd = endBounds.position;
                    Vector3 turn1 = new Vector3(bpBase.x, (bpBase.y + bpEnd.y) * 0.5f, bpBase.z);
                    Vector3 turn2 = new Vector3(bpEnd.x, (bpBase.y + bpEnd.y) * 0.5f, bpEnd.z);

                    // Draw a line between the 2 for now
                    LineRenderer lr = Instantiate(lineRendPrefab, transform).GetComponent<LineRenderer>();
                    lr.positionCount = 4;
                    lr.SetPosition(0, bpBase);
                    lr.SetPosition(1, turn1);
                    lr.SetPosition(2, turn2);
                    lr.SetPosition(3, bpEnd);

                    lineRends.Add(lr);
                }
            }
        }
    }
}
