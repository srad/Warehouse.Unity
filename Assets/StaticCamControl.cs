using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticCamControl : MonoBehaviour
{
    public Camera cam;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(Screenshot.TakeScreenshot(cam.name.ToLower(), Screenshot.ScreenshotPrefix, cam));
        }
    }
}
