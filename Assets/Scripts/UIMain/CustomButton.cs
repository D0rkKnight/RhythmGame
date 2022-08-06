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
    public SpriteRenderer outline;

    public bool pulsing = false;
    public GameObject pulseBG;

    private void Start()
    {
        StartCoroutine("pulse");
    }

    public float bgOpacity {
        get { return bg.color.a; }
        set { 
            bg.color = new Vector4(bg.color.r, bg.color.g, bg.color.b, value);
            txt.color = new Vector4(txt.color.r, txt.color.g, txt.color.b, value > 0 ? 1.0f : 0f); // Text has binary opacity
        }
    }

    public float outlineOpacity
    {
        get { return outline.color.a; }
        set
        {
            outline.color = new Vector4(outline.color.r, outline.color.g, outline.color.b, value);
        }
    }

    private IEnumerator pulse()
    {
        while (true)
        {
            if (pulsing)
            {
                GameObject bg = Instantiate(pulseBG, transform);
                bg.transform.localPosition = new Vector3(0, 0, 3);
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
