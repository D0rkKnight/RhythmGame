using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Scoreboard : MonoBehaviour
{

    public TMP_Text header;
    public TMP_Text scores;
    public TMP_Text footer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void loadScores(string name)
    {
        Ledger l = GameManager.sing.highscores.Find((Ledger l) =>
        {
            return l.name.Equals(name);
        });

        if (l != null)
        {
            string o = "";
            foreach (int score in l.scores)
                o = score + "\n" + o;

            scores.text = o;
        }
    }
}
