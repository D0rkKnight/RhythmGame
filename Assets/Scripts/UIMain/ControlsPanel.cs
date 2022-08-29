using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ControlsPanel : MonoBehaviour
{

    public GameObject[] colInput;
    public GameObject pauseInput;
    public Button defInputButton;
    public YesNoPopup popup;
    [SerializeField] private GameObject clickBlocker;

    public bool selectingKey = false;
    private Action<KeyCode> onSelect;


    // Start is called before the first frame update
    void Start()
    {
        defInputButton.onClick.AddListener(() =>
        {
            GameManager.sing.pushPanelStack(popup.gameObject, false);

            popup.yesBut.GetComponent<IndependentClickCB>().cb = () => {
                resetDefaults();
                updateButtons();
            };
        });

        updateButtons();
    }

    // Update is called once per frame
    void Update()
    {
        if (selectingKey)
        {
            if (Input.anyKeyDown)
            {
                // Do defaults later once input is standardized
                KeyCode newKey = KeyCode.None;

                // Search for which key was pressed
                foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKey(key))
                    {
                        newKey = key;
                        break;
                    }
                }

                onSelect(newKey);
                GameManager.sing.popPanelStack(); // Release the click blocker
                selectingKey = false;

                // Make sure this doesn't proc other behavior
                InputManager.banKey(newKey);

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
            col.transform.Find("Button/Text").GetComponent<TMP_Text>().text = GameManager.getColKey(i).ToString();
        }

        pauseInput.transform.Find("Button/Text").GetComponent<TMP_Text>().text = MusicPlayer.sing.pauseKey.ToString();
    }

    private void queueKeyChange()
    {
        GameManager.sing.pushPanelStack(clickBlocker, false);
        selectingKey = true;
    }

    public void selectCol(int col)
    {
        onSelect = (KeyCode newKey) =>
        {
            GameManager.setColKey(col, newKey);
        };

        queueKeyChange();
    }

    public void selectPause()
    {
        onSelect = (KeyCode newKey) =>
        {
            MusicPlayer.sing.pauseKey = newKey;
        };

        queueKeyChange();
    }

    public void resetDefaults()
    {
        InputManager.resetDefaults();
        updateButtons();
    }
}
