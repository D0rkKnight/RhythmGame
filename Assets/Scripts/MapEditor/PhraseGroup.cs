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
}
