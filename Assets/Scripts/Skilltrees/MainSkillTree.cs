using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using TMPro;

public class MainSkillTree : SkillTree
{

    [System.Serializable]
    public class buttonPair
    {
        public NODE node;
        public SkillButton btn;
        public NODE[] prereqs;
        public int cost = 100;
        public float heatReq = 0;
        public float tokenMult = 1f;

        private MainSkillTree owner;

        public void init(MainSkillTree owner_)
        {
            owner = owner_;
            btn.btn.onClick.AddListener(onClick);

            // Set label text
            btn.GetComponentInChildren<TMP_Text>().text += " $" + cost.ToString();

            checkPrereqs();
        }

        private void onClick()
        {
            // Not enough money or already purchased
            if (owner.tokens < cost || owner.purchasedFlags[(int) node]) return; 
            owner.Tokens -= cost;

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

    public TMP_Text tokenText;
    private int tokens = 0;
    public int Tokens
    {
        get { 
            return tokens; 
        }
        set
        {
            if (tokens != value)
                recompileQueued = true;

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

    public GameObject audViz;

    public buttonPair firstSkillNode; // Picks a first skill node to pulse

    public float tokenMult = 1f;

    public override void init()
    {
        base.init();

        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].init(this);
        }

        lineRends = new List<LineRenderer>();

        // Grab the first skill node
        firstSkillNode = null;
        foreach (buttonPair pair in nodes)
        {
            if (pair.prereqs.Length == 0 && (firstSkillNode == null || pair.cost < firstSkillNode.cost))
            {
                Debug.Log("Assigned");
                firstSkillNode = pair;
            }
        }

        // Sets initial token text
        Tokens = Tokens;

        // Set skill heat colors
        foreach (buttonPair but in nodes) {
            Image heatImg = but.btn.heatImg;

            float a = heatImg.color.a;
            heatImg.color = HeatController.sing.getHeatCol(but.heatReq);
        }
    }

    public void Update()
    {
        base.Update();

        // Weird switch structure
        bool pulsing = firstSkillNode.btn.pulsing;
        if (tokens >= firstSkillNode.cost && !pulsing)
        {
            // Begin pulsing
            pulsing = true;
        } 
        else if (tokens < firstSkillNode.cost && pulsing)
        {
            pulsing = false;
        }
        if (purchasedFlags[(int)firstSkillNode.node])
            pulsing = false;

        firstSkillNode.btn.pulsing = pulsing;

        regenLines(); // Redundancy
    }



    protected override void setActiveFlags()
    {
        bool[] nodeMask = new bool[(int) NODE.SENTINEL];

        // Find active flags
        // TODO: Make UI a separate process, maybe delegated to the buttons
        for (int i = 0; i < nodes.Length; i++)
        {
            buttonPair nodeData = nodes[i];
            int index = (int)nodeData.node;

            float bgOpacity = 0; // Unpurchased items have an opacity of 0
            float outlineOpacity = 0;

            // Skip unpurchased nodes
            if (purchasedFlags[index])
            {
                activeFlags[index] = nodeData.heatReq <= HeatController.sing.Heat || activateAll;
                nodeMask[index] = true;

                // Set opacity of purchased buttons
                bgOpacity = activeFlags[index] ? 1 : 0.5f;
                outlineOpacity = 1;
            }

            // Check if node is an adjacent element
            else
            {
                bool adj = nodeData.prereqs.Length == 0; // No prereqs
                foreach (NODE prereq in nodeData.prereqs) // Purchased prereq
                    if (purchasedFlags[(int)prereq])
                    {
                        adj = true; // Set to quarter opacity
                        break;
                    }

                if (adj)
                {
                    bgOpacity = 0.25f;

                    if (tokens >= nodeData.cost) bgOpacity = 0.5f; // Highlight when purchasable
                }
            }

            // Just disable the button, I think interactable will always grey it out
            nodeData.btn.btn.enabled = tokens >= nodeData.cost && !purchasedFlags[index]; 

            nodeData.btn.bgOpacity = bgOpacity;
            nodeData.btn.outlineOpacity = outlineOpacity;
            nodeData.btn.HeatOpacity = bgOpacity;

            // Set heat text
            nodeData.btn.heatText.text = "" + nodeData.heatReq;
        }

        // Fill in values not dictated by buttons
        for (int i=0; i<(int) NODE.SENTINEL; i++)
        {
            if (!nodeMask[i] && purchasedFlags[i]) activeFlags[i] = true;
        }
    }

    protected override void compileMech()
    {
        base.compileMech();

        // Meta elements
        transform.Find("Canvas").gameObject.SetActive(activeFlags[(int)NODE.SKILLTREE]);

        if (audViz != null)
            audViz.SetActive(activeFlags[(int)NODE.AUD_VIZ]);

        // Config Musicplayer widgets
        Transform mpTrans = MusicPlayer.sing.transform;

        mpTrans.Find("Canvas/Score").gameObject.SetActive(activeFlags[(int)NODE.SCORE]);
        mpTrans.Find("Canvas/Combo").gameObject.SetActive(activeFlags[(int)NODE.COMBO]);
        mpTrans.Find("Canvas/Heat").gameObject.SetActive(activeFlags[(int)NODE.HEAT]);

        // Set token multiplier
        tokenMult = 1f;
        foreach (buttonPair but in nodes)
        {
            if (activeFlags[(int)but.node])
                tokenMult *= but.tokenMult;
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
        // Might want to optimize if this ever becomes an issue

        // Clear old lines
        foreach (LineRenderer lr in lineRends) Destroy(lr);
        lineRends.Clear();

        // Gen new lines
        foreach (buttonPair bp in nodes)
        {
            // Check that every prereq is fulfilled
            bool allPrereqs = true;
            foreach (NODE prereq in bp.prereqs)
            {
                if (!purchasedFlags[(int) prereq])
                {
                    allPrereqs = false;
                    break;
                }

            }
            if (!allPrereqs) // Skip invalid nodes
                continue;

            // Draw lines between active skills (from an active skill to its parent)
            // 4 points: base, turn 1, turn 2, end

            RectTransform bpBounds = bp.btn.GetComponent<RectTransform>();
            Vector3 bpBase = bpBounds.position;

            foreach (NODE prereq in bp.prereqs)
            {
                // Find node to hook to (going upstream)
                Button target = null;
                foreach (buttonPair search in nodes) if (search.node == prereq)
                    {
                        target = search.btn.btn;
                        break;
                    }

                RectTransform endBounds = target.GetComponent<RectTransform>();
                Vector3[] sCorn = new Vector3[4];
                Vector3[] eCorn = new Vector3[4];

                endBounds.GetWorldCorners(eCorn);
                bpBounds.GetWorldCorners(sCorn);

                float endY = eCorn[0].y;
                float startY = sCorn[2].y;

                Vector3 bpEnd = endBounds.position;
                Vector3 turn1 = new Vector3(bpBase.x, (bpBase.y + bpEnd.y) * 0.5f, bpBase.z);
                Vector3 turn2 = new Vector3(bpEnd.x, (bpBase.y + bpEnd.y) * 0.5f, bpEnd.z);

                // Draw a line between the 2 for now
                LineRenderer lr = Instantiate(lineRendPrefab, transform).GetComponent<LineRenderer>();
                lr.positionCount = 4;
                lr.SetPosition(0, new Vector3(bpBase.x, startY, bpBase.z));
                lr.SetPosition(1, turn1);
                lr.SetPosition(2, turn2);
                lr.SetPosition(3, new Vector3(bpEnd.x, endY, bpEnd.z));

                // set lr opacity to source btn opacity
                Color col = Color.white * bp.btn.bgOpacity;
                col.a = 1f;

                lr.startColor = col;
                lr.endColor = col;

                lineRends.Add(lr);
            }
        }
    }

    public void addSubtokens(float amt)
    {
        SubToken += (int) (tokenMult * amt);
    }
}
