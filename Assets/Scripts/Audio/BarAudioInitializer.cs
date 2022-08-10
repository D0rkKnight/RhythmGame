using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarAudioInitializer : MonoBehaviour
{
    // Creates an array of bars that listen to audio
    public int barCount = 12;
    public GameObject barPrefab;

    public Transform leftTrans;
    public Transform rightTrans;

    private BarAudioVisualizer[] bars;

    // Start is called before the first frame update
    void Start()
    {
        bars = new BarAudioVisualizer[barCount];

        for (int i=0; i<barCount; i++)
        {
            Transform _bar = Instantiate(barPrefab, transform).transform;
            float ratio = i / (float)barCount;

            _bar.position = leftTrans.position + (rightTrans.position - leftTrans.position) * ratio;
            Vector3 newScale = _bar.localScale;
            newScale.x = ratio * (rightTrans.position - leftTrans.position).x;
            _bar.localScale = newScale;

            bars[i] = _bar.GetComponent<BarAudioVisualizer>();
            bars[i].bias = i/(float) barCount * 2.0f;
            bars[i].beatScale.y = 1 + (i/(float) barCount);
            bars[i].beatScale.x = newScale.x;
            bars[i].restScale.x = newScale.x;

            bars[i].freqBand = i * 12 / barCount;

            SpriteRenderer _sr = _bar.GetComponent<SpriteRenderer>();
            var _col = _sr.color;
            _col.a = 0.3f;
            _sr.color = _col;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
