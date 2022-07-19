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
        string segment = getEnclosed('(', ')');

        string[] meta = segment.Split(',');
        for (int i = 0; i < meta.Length; i++) 
            meta[i] = meta[i].Trim();

        return meta;
    }

    public string getEnclosed(char start, char end)
    {
        string segment = "";
        // Look for meta start symbol
        if (ptr < str.Length && str[ptr] == start)
        {
            ptr++;
            while (ptr < str.Length && str[ptr] != end)
            {
                segment += str[ptr];
                ptr++;
            }
            // Shift pointer onto start of next block
            ptr++;
        }

        return segment;
    }

    public char peekChar()
    {
        return str[ptr];
    }
}
