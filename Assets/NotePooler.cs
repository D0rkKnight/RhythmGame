using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Handles recycling of note objects
public class NotePooler : MonoBehaviour
{
    public static NotePooler Instance { get; private set;}

    public Dictionary<string, List<Note>> pools; // Prefab key, cache value

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null)
            throw new Exception("Notepooler singleton broken");

        Instance = this;

        pools = new Dictionary<string, List<Note>>();
    }

    public void addToCache(Note n)
    {
        n.gameObject.SetActive(false);
        n.transform.parent = transform;

        if (!pools.ContainsKey(n.catName))
            pools.Add(n.catName, new List<Note>());

        pools[n.catName].Add(n);
    }

    public Note getFromCache(string cat)
    {
        if (!pools.ContainsKey(cat) || pools[cat].Count == 0)
            return null;

        List<Note> l = pools[cat];
        Note n = l[l.Count - 1];
        l.RemoveAt(l.Count - 1);

        n.gameObject.SetActive(true);
        n.onRecycle();

        return n;
    }
}
