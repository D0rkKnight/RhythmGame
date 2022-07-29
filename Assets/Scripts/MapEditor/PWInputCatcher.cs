using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PWInputCatcher : MonoBehaviour, Clickable
{
    [SerializeField] private WorkspaceEditor workspace;

    public int onClick(int code)
    {
        // Spawn a note
        if (code == 0)
        {
            if (MapEditor.sing.InteractMode == MapEditor.MODE.WRITE)
                workspace.addPhraseEntry();
        }

        return 1;
    }

    public int onOver()
    {
        return 1;
    }
}
