using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager sing;

    public GameObject settings;

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Gamemanager Singleton broken (very bad)");

        Phrase.init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void controlSettings(bool val)
    {
        settings.SetActive(val);
    }

    public void changeScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }
}
