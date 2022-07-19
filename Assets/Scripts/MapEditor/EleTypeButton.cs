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
        // Rebuild the element
        // All metadata will be nulled upon doing this

        Phrase p = MapEditor.sing.activePhrase;
        MapEditor.sing.activePhrase = Phrase.staticCon(p.lane, p.beat, p.accent, null, ele);
        MapEditor.sing.updateMetaField();
    }
}
