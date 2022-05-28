using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorSkillTree : SkillTree
{
    public Toggle[] toggles = new Toggle[(int) NODE.SENTINEL];
    public GameObject togglePrefab;

    public override void init()
    {
        // Don't do base init
        Transform par = transform.Find("Canvas/Scroll View/Viewport/Content");

        // Generate toggles
        for(int i=0; i<(int)NODE.SENTINEL-1; i++)
        {
            GameObject obj = Instantiate(togglePrefab, par);

            // Positions should be automatically laid out
            //obj.transform.localPosition = new Vector3(100, -30 * (i + 1), 0);

            toggles[i] = obj.GetComponent<Toggle>();

            string txt = Enum.GetName(typeof(NODE), i); // Get name of enum
            toggles[i].GetComponentInChildren<Text>().text = txt;

            // Set recompile on any toggle chage
            toggles[i].onValueChanged.AddListener(delegate
            {
                compile();
            });
        }
    }

    protected override void setActiveFlags()
    {
        for (int i = 0; i < (int)NODE.SENTINEL-1; i++)
            activeFlags[i] = toggles[i].isOn;
    }

    protected override void enableNewOptions()
    {
        // Do nothing
    }
}
