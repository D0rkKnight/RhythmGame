using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

            // Kill if click blocker
            if (hit.transform.tag == "MouseBlocker") break;
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
