using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// 0 means block cast, 1 means pass cast to lower layers
public class InputManager : MonoBehaviour
{
    public bool focusedInField;
    public List<InputField> fields = new List<InputField>();
    public static InputManager sing;
    private void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;
    }
    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

        // Order hits by z
        Array.Sort(hits, (x, y) =>
        {
            float zx = x.transform.position.z;
            float zy = y.transform.position.z;
            if (zx > zy) return 1;
            if (zx < zy) return -1;
            return 0;
        });

        // Scanning for clicks and hovers are independent processes
        bool scanningClicks = true;
        bool scanningHover = true;
        foreach (RaycastHit2D hit in hits)
        {
            foreach (MonoBehaviour mono in hit.transform.GetComponents<Clickable>())
            {
                if (!mono.isActiveAndEnabled) continue; // Skip sleeping components

                Clickable clk = (Clickable)mono;

                if (scanningClicks && Input.GetMouseButtonDown(0)) 
                    if (clk.onClick(0) == 0) scanningClicks = false;
                if (scanningClicks && Input.GetMouseButtonDown(1))
                    if (clk.onClick(1) == 0) scanningClicks = false;

                if (scanningHover && clk.onOver() == 0) scanningHover = false;
            }

            // Kill if click blocker or blocked
            if (hit.transform.tag == "MouseBlocker" || (!scanningClicks && !scanningHover)) break;
        }

        focusedInField = false;
        foreach (InputField f in fields) if (f.isFocused) focusedInField = true;
    }

    public static bool checkKeyDown(KeyCode c)
    {
        if (!sing.focusedInField && Input.GetKeyDown(c)) return true;
        return false;
    }
}
