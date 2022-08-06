using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPulser : MonoBehaviour
{
    public float life = 1f;
    public float scaleTo = 1.5f;
    private Vector3 targetScale;

    public float scaleSpeed = 3f;

    private SpriteRenderer rend;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, life);

        rend = GetComponent<SpriteRenderer>();
        targetScale = transform.localScale * scaleTo;
        targetScale.z = 1;
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

        Color c = rend.color;
        c.a = Mathf.Lerp(rend.color.a, 0, Time.deltaTime * scaleSpeed);
        rend.color = c;
    }
}
