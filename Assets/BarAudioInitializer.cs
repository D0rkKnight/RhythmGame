using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarAudioInitializer : MonoBehaviour
{
    // Creates an array of bars that listen to audio
    public int barCount = 12;
    public GameObject barPrefab;

    private BarAudioVisualizer[] bars;

    // Start is called before the first frame update
    void Start()
    {
        bars = new BarAudioVisualizer[barCount];

        for (int i=0; i<barCount; i++)
        {
            Transform _bar = Instantiate(barPrefab, transform).transform;
            _bar.position = transform.position + Vector3.right * i;

            bars[i] = _bar.GetComponent<BarAudioVisualizer>();
            bars[i].bias = i/(float) barCount * 2.0f;
            bars[i].beatScale.y = 1 + (i/(float) barCount);
            bars[i].freqBand = i * 4 / barCount;

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
