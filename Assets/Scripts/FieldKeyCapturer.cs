using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class FieldKeyCapturer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        InputManager.sing.fields.Add(GetComponent<InputField>());
    }
}
