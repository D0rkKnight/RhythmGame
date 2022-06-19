using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class StringScanner
{
    public string str;
    public int ptr;

    public StringScanner(string str_)
    {
        this.str = str_;
        ptr = 0;
    }

    public string getSegment(List<char> charlist)
    {
        string o = "";
        while (ptr < str.Length)
        {
            if (charlist.Contains(str[ptr])) o += str[ptr];
            else break;

            ptr++;
        }

        return o;
    }

    public string[] getMeta()
    {
        string segment = "";
        // Look for meta start symbol
        Debug.Log(str[ptr]);
        if (ptr < str.Length && str[ptr] == '(')
        {
            ptr++;
            while (ptr < str.Length && str[ptr] != ')')
            {
                segment += str[ptr];
                ptr++;
            }
            // Shift pointer onto start of next block
            ptr++;
        }

        string[] meta = segment.Split(',');
        for (int i = 0; i < meta.Length; i++) 
            meta[i] = meta[i].Trim();

        return meta;
    }
}
