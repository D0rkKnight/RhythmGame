using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PanelInteractor : MonoBehaviour
{

    public enum MODE
    {
        PUSH, POP
    }
    public MODE mode;

    public GameObject panel;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (mode == MODE.PUSH)
            {
                GameManager.sing.pushPanelStack(panel);
            }
            else if (mode == MODE.POP)
            {
                GameManager.sing.popPanelStack();
            }
        });
    }
}
