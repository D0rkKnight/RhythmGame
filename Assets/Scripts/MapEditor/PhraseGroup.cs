using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PhraseGroup
{
    public List<Phrase> phrases;
    public string name;

    public PhraseGroup(List<Phrase> phrases_, string name_)
    {
        phrases = phrases_;
        name = name_;
    }

    public PhraseGroup clone()
    {
        List<Phrase> newPhrases = new List<Phrase>();
        foreach (Phrase p in phrases)
            newPhrases.Add(p.hardClone());

        return new PhraseGroup(newPhrases, name);
    }
}
