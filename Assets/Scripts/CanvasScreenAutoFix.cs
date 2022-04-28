using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasScreenAutoFix : MonoBehaviour
{
    public static CanvasScreenAutoFix instance;

    private static float DEPEND_W = 1080;
    private static float DEPEND_H = 1920;

    public float scaleRatio = 1.0f;


    private void ResizeCanvas()
    {
        int screenW = Screen.width;
        int screenH = Screen.height;
        if (DEPEND_W == screenW && DEPEND_H == screenH)
        {
            scaleRatio = 1.0f;
            return;
        }
        else
        {
            float W_scale = screenW / (float)DEPEND_W;
            float H_scale = screenH / (float)DEPEND_H;
            float scale = W_scale < H_scale ? W_scale : H_scale;

            GetComponent<CanvasScaler>().scaleFactor = scale;

            scaleRatio = scale;
        }
    }

    private void Awake()
    {
        ResizeCanvas();
        if (instance == null)
        {
            instance = this;
        }
    }

}