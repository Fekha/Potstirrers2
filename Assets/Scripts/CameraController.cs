using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{

    // Set this to the in-world distance between the left & right edges of your scene.
    public float sceneWidth = 10;
    public float horizontalFOV = 120f;
    //float targetaspect = 9.0f / 16.0f;

    //// determine the game window's current aspect ratio
    //float windowaspect = (float)Screen.width/ (float)Screen.height;
    Camera _camera;
    void Start()
    {
        _camera = GetComponent<Camera>();
    }

    // Adjust the camera's height so the desired scene width fits in view
    // even if the screen/window size changes dynamically.
    void Update()
    {
        _camera.fieldOfView = calcVertivalFOV(horizontalFOV, Camera.main.aspect);
        //float unitsPerPixel = sceneWidth / Screen.width;

        //float desiredHalfHeight = 0.5f * unitsPerPixel * Screen.height;

        //_camera.orthographicSize = desiredHalfHeight;

        //float scaleheight = windowaspect / targetaspect;


        //// if scaled height is less than current height, add letterbox
        //if (scaleheight < 1.0f)
        //{
        //    Rect rect = _camera.rect;

        //    rect.width = 1.0f;
        //    rect.height = scaleheight;
        //    rect.x = 0;
        //    rect.y = (1.0f - scaleheight) / 2.0f;

        //    _camera.rect = rect;
        //}
        //else // add pillarbox
        //{
        //    float scalewidth = 1.0f / scaleheight;

        //    Rect rect = _camera.rect;

        //    rect.width = scalewidth;
        //    rect.height = 1.0f;
        //    rect.x = (1.0f - scalewidth) / 2.0f;
        //    rect.y = 0;

        //    _camera.rect = rect;
        //}
    }

    private float calcVertivalFOV(float hFOVInDeg, float aspectRatio)
    {
        float hFOVInRads = hFOVInDeg * Mathf.Deg2Rad;
        float vFOVInRads = 2 * Mathf.Atan(Mathf.Tan(hFOVInRads / 2) / aspectRatio);
        float vFOV = vFOVInRads * Mathf.Rad2Deg;
        return vFOV;
    }


    // current viewport height should be scaled by this amount

}
