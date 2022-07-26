using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager sing;

    public GameObject settings;
    public KeyCode[] colKeys;

    public Stack<GameObject> panelStack = new Stack<GameObject>(); // Tracks the active stack of ui panels
    public GameObject activePanel = null;

    // Start is called before the first frame update
    void Awake()
    {
        if (sing != null) Debug.LogError("Gamemanager Singleton broken (very bad)");
        sing = this;

        Phrase.init();
    }

    private void Start()
    {
        // Hide all ui panels
        foreach (Transform child in transform.Find("Canvas"))
        {
            if (child.tag == "UIPanel")
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void changeScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void pushPanelStack(GameObject obj)
    {
        // Deactivate last element
        if (panelStack.Count > 0)
            panelStack.Peek().SetActive(false);

        // Activate new element
        panelStack.Push(obj);
        obj.SetActive(true);
    }

    public GameObject popPanelStack()
    {
        // Deactivate top element
        GameObject top = panelStack.Pop();
        if (top != null) top.SetActive(false);

        // Activate next element
        if (panelStack.Count > 0)
            panelStack.Peek().SetActive(true);

        return top;
    }
}
