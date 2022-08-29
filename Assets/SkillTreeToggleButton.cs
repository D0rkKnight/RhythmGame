using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreeToggleButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Timeliner.sing.mpCentered = !Timeliner.sing.mpCentered; // Flip toggle
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
