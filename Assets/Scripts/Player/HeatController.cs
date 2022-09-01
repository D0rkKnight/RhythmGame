using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeatController : MonoBehaviour
{

    private float heat = 0; // Spiciness levels
    public float Heat
    {
        get { 
            if (!SkillTree.sing.activeFlags[(int)SkillTree.NODE.HEAT])
                return 0;
            return heat;
        }
        set
        {
            heat = Mathf.Clamp(value, 0, maxHeat);

            if (capHeat) heat = maxHeat;

            if (!SkillTree.sing.activeFlags[(int)SkillTree.NODE.HEAT])
                heat = 0;

            // Recompile skill tree
            SkillTree.sing.compile();
        }
    }
    private float visualHeat = 0; // How much heat it looks like you have

    public bool capHeat = false;

    public float heatTierDist = 30; // How much heat to tier up once
    public float maxHeat = 100; // Maximum heat achievable
    public Transform bar1;
    public Transform bar2;
    public TMPro.TMP_Text text;
    public SpriteRenderer icon;

    public Color[] colors;
    public static HeatController sing;

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;
    }

    private void Start()
    {
        Heat = Heat; // Update heat
    }

    // Update is called once per frame
    void Update()
    {
        visualHeat = Mathf.Lerp(visualHeat, heat, Time.deltaTime*7);

        float barFill = (visualHeat % heatTierDist)/heatTierDist;
        int tier = (int) (visualHeat / heatTierDist);

        // Bound tier color
        tier = Mathf.Min(tier, colors.Length - 2);

        // If past max, set bar to full
        if (visualHeat > heatTierDist * (colors.Length-1)) barFill = 1;
        
        Color bgCol = colors[tier];
        Color barCol = colors[tier + 1];

        bar1.GetComponent<SpriteRenderer>().color = bgCol;
        bar2.GetComponent<SpriteRenderer>().color = barCol;

        bar2.localScale = new Vector3(1, barFill, 1);


        text.text = heat.ToString();
        icon.color = getHeatCol(heat);
    }

    public Color getHeatCol(float heat_)
    {
        return colors[(int)(heat_ / heatTierDist)];
    }
}
