using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EleTypeButton : MonoBehaviour
{
    public Phrase.TYPE ele;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(setElement);
    }

    private void setElement()
    {
        Phrase p = MapEditor.sing.activePhrase;
        MapEditor.sing.activePhrase = Phrase.staticCon(p.lane, p.partition, p.beat, p.accent, p.wait, p.dur, ele);
        MapEditor.sing.updateMetaField();
    }
}
