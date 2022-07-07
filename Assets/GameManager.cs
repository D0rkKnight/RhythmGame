using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager sing;

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
}
