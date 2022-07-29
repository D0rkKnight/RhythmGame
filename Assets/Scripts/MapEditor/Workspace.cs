using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Workspace
{
    public List<BeatRow> rows;
    public PhraseGroup group;

    public Workspace(List<BeatRow> rows_, PhraseGroup group_)
    {
        rows = rows_;
        group = group_;
    }

    public void sortRows()
    {
        rows.Sort((x, y) =>
        {
            Phrase px = x.slots[0].phrase;
            Phrase py = y.slots[0].phrase;

            if (px.beat > py.beat) return 1;
            if (px.beat < py.beat) return -1;
            return 0;
        });
    }
}
