using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// https://www.sejuku.net/blog/82841
public class StatusText : MonoBehaviour
{
    private int frameCount;
    private float prevTime;    
    private Text uiText;

    [SerializeField, Header("追加のステータス保持者")]
    private MonoBehaviour controller;

    // Start is called before the first frame update
    void Start()
    {
        uiText = GetComponent<Text>();
        frameCount = 0;
        prevTime = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (uiText == null) return;

        frameCount++;
        float time = Time.realtimeSinceStartup - prevTime;

        if (time < 0.5f) return;
        float fps = frameCount / time;

        uiText.text = string.Format(" FPS:{0:0.0} {1}", fps, controller);

        frameCount = 0;
        prevTime = Time.realtimeSinceStartup;
    }
}
