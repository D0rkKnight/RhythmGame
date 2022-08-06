using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ControlsPanel : MonoBehaviour
{

    public GameObject[] colInput;
    [SerializeField] private GameObject clickBlocker;

    public int selectedCol = -1;


    // Start is called before the first frame update
    void Start()
    {

        updateButtons();
    }

    // Update is called once per frame
    void Update()
    {
        if (selectedCol >= 0)
        {
            if (Input.anyKeyDown)
            {
                KeyCode newKey = GameManager.sing.colKeys[selectedCol];

                // Search for which key was pressed
                foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKey(key))
                    {
                        newKey = key;
                        break;
                    }
                }

                GameManager.sing.colKeys[selectedCol] = newKey;
                selectedCol = -1;
                GameManager.sing.popPanelStack(); // Release the click blocker

                // Proc a visual update
                updateButtons();
                NoteColumn.Regenerate();
            }
        }
    }

    private void updateButtons()
    {
        // Set labels
        for (int i = 0; i < colInput.Length; i++)
        {
            GameObject col = colInput[i];

            // Labels and keycodes
            col.transform.Find("Label").GetComponent<TMP_Text>().text = "COL " + (i + 1);
            col.transform.Find("Button/Text").GetComponent<TMP_Text>().text = GameManager.sing.colKeys[i].ToString();
        }
    }

    public void selectCol(int col)
    {
        selectedCol = col;
        GameManager.sing.pushPanelStack(clickBlocker, false);
    }
}
