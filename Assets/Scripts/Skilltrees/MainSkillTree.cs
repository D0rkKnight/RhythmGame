using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

public class MainSkillTree : SkillTree
{

    [System.Serializable]
    public class buttonPair
    {
        public NODE node;
        public Button btn;
        public NODE[] prereqs;
        public int cost = 100;
        public float heatReq = 0;

        private MainSkillTree owner;

        public buttonPair(NODE node_, Button btn_)
        {
            node = node_;
            btn = btn_;
        }

        public void init(MainSkillTree owner_)
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

    public Text tokenText;
    private int tokens = 0;
    public int Tokens
    {
        get { return tokens; }
        set
        {
            tokens = value;
            if (tokenText != null)
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

    public override void init()
    {
        base.init();

        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].init(this);
        }

        lineRends = new List<LineRenderer>();
    }

    protected override void setActiveFlags()
    {
        // Find active flags
        for (int i = 0; i < (int)NODE.SENTINEL - 1; i++)
        {
            buttonPair nodeData = nodes[i];
            int index = (int)nodeData.node;

            // Skip unpurchased nodes
            if (!purchasedFlags[index]) continue;

            activeFlags[index] = nodeData.heatReq <= HeatController.sing.Heat || activateAll;

            // Set opacity of buttons
            float opacity = activeFlags[index] ? 1 : 0.5f;
            Image img = nodeData.btn.GetComponent<Image>();
            img.color = new Color(img.color.r, img.color.g, img.color.b, opacity);
        }
    }

    protected override void enableNewOptions()
    {
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
