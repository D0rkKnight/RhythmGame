using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PWInputCatcher : MonoBehaviour, Clickable
{
    [SerializeField] private PhraseWorkspace workspace;

    public int onClick(int code)
    {
        // Spawn a note
        if (code == 0)
        {
            workspace.addPhraseEntry();
        }

        return 1;
    }

    public int onOver()
    {
        return 1;
    }
}
