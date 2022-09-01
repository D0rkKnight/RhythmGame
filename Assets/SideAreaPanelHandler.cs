using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideAreaPanelHandler : MonoBehaviour
{
    [System.Serializable]
    public class panelPair
    {
        public GameObject panel;
        public CustomButton btn;

        public void select()
        {
            panel.SetActive(true);
        }

        public void deselect()
        {
            panel.SetActive(false);
        }
    }

    public panelPair[] pairs;
    public panelPair activePair = null;

    // Start is called before the first frame update
    void Start()
    {
        activePair = null;

        foreach (var pair in pairs)
        {
            pair.btn.btn.onClick.AddListener(() =>
            {
                // Deselect last pair
                if (activePair != null)
                    activePair.deselect();

                activePair = pair;
                activePair.select();
            });
        }

        foreach (var pair in pairs)
            pair.deselect();

        pairs[0].select();
        activePair = pairs[0];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
