using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleNote : Note
{
    public TMPro.TMP_Text label;

    public int node;
    public bool val;

    public override bool checkMiss(MusicPlayer mp, float dt)
    {
        return false; // Can't miss these
    }

    public override void onCross()
    {
        base.onCross();

        SkillTree.sing.toggleFlags[node] = val;
        SkillTree.sing.compile();
    }
}
