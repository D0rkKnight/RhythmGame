using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

public class SaveSelectButton : MonoBehaviour, Clickable
{
    public string save = "none";
    public CustomButton custBut;
    public Button delBut;

    public int framesSinceHov = 10;

    // Start is called before the first frame update
    void Start()
    {
        // Assign save info
        custBut.btn.onClick.AddListener(() =>
        {
            GameManager.activeSave = save;
            GameManager.sing.changeScene("Scenes/MainScene");
        });

        delBut.gameObject.SetActive(false);

        delBut.onClick.AddListener(() =>
        {
            string fpath = Path.Combine(Application.streamingAssetsPath, "Saves", save + ".txt");
            File.Delete(fpath);
            File.Delete(fpath+".meta"); // Delete meta file as well
        });
    }

    private void Update()
    {
        delBut.gameObject.SetActive(framesSinceHov <= 1);

        framesSinceHov++;
    }

    public int onClick(int code)
    {
        return 1;
    }

    public int onOver()
    {
        framesSinceHov = 0;
        return 1;
    }
}
