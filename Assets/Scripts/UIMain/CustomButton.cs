using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class CustomButton : MonoBehaviour
{

    public Image bg;
    public TMP_Text txt;
    public Button btn;

    public float Opacity {
        get { return bg.color.a; }
        set { 
            bg.color = new Vector4(bg.color.r, bg.color.g, bg.color.b, value);
            txt.color = new Vector4(txt.color.r, txt.color.g, txt.color.b, value > 0 ? 1.0f : 0f); // Text has binary opacity
        }
    }
}
