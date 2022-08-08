using System.Collections;
using UnityEngine;
using System.Collections.Generic;
public class Ledger
{
    public string name;
    public List<int> scores;

    public Ledger(string name_, List<int> scores_)
    {
        name = name_;
        scores = scores_;

        scores.Sort();
    }
}