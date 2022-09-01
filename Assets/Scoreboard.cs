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
        bool active = MusicPlayer.sing.state == MusicPlayer.STATE.INTERIM;
        header.gameObject.SetActive(active);
        scores.gameObject.SetActive(active);
        footer.gameObject.SetActive(active);
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
            for (int i = l.scores.Count-1; i >= 0; i--)
            {
                o += l.scores[i];
                if (i == l.scores.Count - 1 && GameManager.sing.wasHighscore) 
                    o += " New Highscore!";
                o += "\n";
            }

            scores.text = o;
        }
    }
}
