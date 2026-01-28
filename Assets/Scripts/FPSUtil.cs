using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class FPSUtil : MonoBehaviour
{
// #if UNITY_ANDROID
    private float _fps = 0f;
    
    // 用于显示帧率的字符串
    private string _fpsDisplay = "FPS: 0";
    
    // 定义GUI样式
    private GUIStyle _guiStyle = new GUIStyle();

    void Start()
    {
        // 设置GUI样式
        _guiStyle.fontSize = 50; // 字体大小
        _guiStyle.normal.textColor = Color.green; // 字体颜色
    }

    
    void Update()
    {
        _fps = 1.0f / Time.deltaTime;

        _fpsDisplay = "FPS: " + Mathf.Round(_fps).ToString();
    }
    

    // 在屏幕上绘制帧率信息
    void OnGUI()
    {
        // 计算右上角的位置
        float xPosition = Screen.width - 250; // 假设标签宽度为200，留出10的边距
        float yPosition = 10; // 距离屏幕顶部的边距

        GUI.Label(new Rect(xPosition, yPosition, 200, 40), _fpsDisplay, _guiStyle);
    }
// #endif
}
