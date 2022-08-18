using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveSelector : MonoBehaviour
{
    public List<CustomButton> saveSlots;

    // Start is called before the first frame update
    void Start()
    {
        // Write data to saveslots

        for (int i=0; i<saveSlots.Count; i++)
        {
            string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", "save" + (i+1) + ".txt");
            CustomButton but = saveSlots[i];

            if (File.Exists(fpath))
            {
                but.txt.text = "Save " + (i+1);
            } else
            {
                but.txt.text = "New Save";
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
