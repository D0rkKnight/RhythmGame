using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Clickable
{
    // Returns 1 for passable and 0 for not passable
    public int onClick(int code);
    public int onOver();
}
