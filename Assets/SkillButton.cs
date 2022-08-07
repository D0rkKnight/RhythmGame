using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : CustomButton
{
    public TMPro.TMP_Text heatText;
    public Image heatImg;

    public float HeatOpacity
    {
        get { return heatImg.color.a;  }
        set
        {
            heatImg.color = new Color(heatImg.color.r, heatImg.color.g, heatImg.color.b, value);
            heatText.color = new Color(heatText.color.r, heatText.color.g, heatText.color.b, value);
        }
    }
}
