using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Workspace
{
    public List<BeatRow> rows;
    public string name;

    public Workspace(List<BeatRow> rows_, string name_)
    {
        rows = rows_;
        name = name_;
    }
}
