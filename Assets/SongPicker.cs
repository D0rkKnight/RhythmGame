using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SongPicker : MonoBehaviour
{
    public Transform scrollRegion;
    public Toggle togglePrefab;
    public List<Toggle> toggles;

    // Start is called before the first frame update
    void Start()
    {
        toggles = new List<Toggle>();

        // Add gamemanager songs to listing

        // Generate toggles
        foreach(string mapItem in GameManager.sing.mapList)
        {
            // Open the map up and get the song name
            Map map = MapSerializer.sing.parseMap(mapItem);

            Toggle toggle = Instantiate(togglePrefab, scrollRegion);
            toggle.GetComponentInChildren<TMPro.TMP_Text>().text = map.name;

            toggle.onValueChanged.AddListener((val) =>
            {
                // Do this since strings seem to work where ints don't
                for (int i = 0; i < GameManager.sing.mapList.Length; i++)
                {
                    if (GameManager.sing.mapList[i].Equals(mapItem))
                    {
                        GameManager.sing.mapBans[i] = !val;
                        break;
                    }
                }

                GameManager.sing.regenerateMapQueue();
                checkToggles();
            });

            toggles.Add(toggle);
        }

    }

    public void checkToggles()
    {
        Toggle lastTog = null;
        int cnt = 0;
        // Needs to discover 2 items to permit the ban
        for (int i = 0; i < GameManager.sing.mapBans.Length; i++) {
            bool item = GameManager.sing.mapBans[i];
            if (!item) {
                cnt++;
                lastTog = toggles[i];
            }

            // Release every toggle as one comes by them
            toggles[i].enabled = true;
        }

        // Lock the last toggle if it is the only one
        if (cnt == 1)
            lastTog.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
