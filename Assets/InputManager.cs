using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{

    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

        foreach (RaycastHit2D hit in hits)
        {
            foreach (Clickable clk in hit.transform.GetComponents<Clickable>())
            {
                if (Input.GetMouseButtonDown(0)) 
                    if (clk.onClick(0) == 0) break;
                if (Input.GetMouseButtonDown(1))
                    if (clk.onClick(1) == 0) break;

                if (clk.onOver() == 0) break;
            }
        }
    }
}
