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

    public static bool[] banList;

    public enum BINDS
    {
        COL1, COL2, COL3, COL_LAST, PAUSE, SENTINEL
    }

    private void Awake()
    {
        if (sing != null) Debug.LogError("Singleton broken");
        sing = this;

        // Init banlist
        banList = new bool[Enum.GetValues(typeof(KeyCode)).Length];
        for (int i = 0; i < banList.Length; i++)
            banList[i] = false;
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


        // Wipe banlist if anything is released
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyUp(key))
                banList[(int)key] = false;
        }
    }

    public static bool checkKeyDown(KeyCode c)
    {
        if (!sing.focusedInField && Input.GetKeyDown(c) && !banList[(int) c]) return true;
        return false;
    }

    public static void banKey(KeyCode c)
    {
        banList[(int)c] = true;
    }

    // For gameplay purposes
    // Will do nothing if UI is on the GM stack
    public static bool checkKeyDownGame(KeyCode c)
    {
        return checkKeyDown(c) && GameManager.sing.panelStack.Count == 0;
    }

    public static void resetDefaults()
    {
        Save s = Save.readFromDisk("main");
        s.readInputsFromSave();
    }
}
