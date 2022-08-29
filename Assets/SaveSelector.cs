using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SaveSelector : MonoBehaviour
{
    public List<SaveSelectButton> saveSlots;
    public YesNoPopup delPopup;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var slot in saveSlots)
        {
            slot.delBut.onClick.AddListener(() =>
            {
                updateSaveUI();
            });

            slot.delBut.gameObject.SetActive(false);

            slot.delBut.onClick.AddListener(() =>
            {
                GameManager.sing.pushPanelStack(delPopup.gameObject, false);

                // Configure delete configure button
                Button yesBut = delPopup.GetComponent<YesNoPopup>().yesBut;
                yesBut.GetComponent<IndependentClickCB>().cb = () =>
                {
                    string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", slot.save + ".txt");
                    File.Delete(fpath);
                    File.Delete(fpath + ".meta"); // Delete meta file as well

                    // Queue text regeneration
                    updateSaveUI();
                };
            });
        }

        // Write data to saveslots
        updateSaveUI();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateSaveUI()
    {
        for (int i = 0; i < saveSlots.Count; i++)
        {
            string saveName = "save" + (i + 1);
            string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", saveName + ".txt");
            SaveSelectButton but = saveSlots[i];

            if (File.Exists(fpath))
            {
                but.custBut.txt.text = "Save " + (i + 1);
            }
            else
            {
                but.custBut.txt.text = "New Save";
            }

            but.save = saveName;
        }
    }
}
