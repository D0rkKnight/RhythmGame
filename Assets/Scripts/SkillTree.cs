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

    public GameObject lineRendPrefab;
    public List<LineRenderer> lineRends;

    // Start is called before the first frame update
    void Start()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        for (int i=0; i<nodes.Length; i++)
        {
            nodes[i].init(this);
        }

        lineRends = new List<LineRenderer>();
    }

    // Recompiles the game state depending on the given skill flags
    public void compile()
    {
        MusicPlayer mp = MusicPlayer.sing;
        MapSerializer ns = MapSerializer.sing;

        mp.columns[0].Active = flags[(int) NODE.L_EXPAND];
        mp.columns[3].Active = flags[(int) NODE.R_EXPAND];

        if (flags[(int)NODE.ACCENT_1]) ns.accentLim++;
        if (flags[(int)NODE.HOLD]) ns.genType[(int) Phrase.TYPE.HOLD] = true;

        // Cleanup lines
        foreach (LineRenderer lr in lineRends) Destroy(lr);
        lineRends.Clear();

        // Update buttons
        foreach (buttonPair bp in nodes)
        {
            // Activate new buttons
            if (!bp.btn.gameObject.activeSelf)
                bp.checkPrereqs();
        }

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
