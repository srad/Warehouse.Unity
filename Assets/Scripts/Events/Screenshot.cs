using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

public class Screenshot : MonoBehaviour
{
    public GameObject panel;
    private string _screenshotPath;

    public void SaveScreenshot()
    {
        _screenshotPath = Path.Combine(Application.dataPath, "Screenshots");
        if (!Directory.Exists(_screenshotPath))
        {
            Directory.CreateDirectory(_screenshotPath);
        }

        panel.gameObject.SetActive(false);

        StartCoroutine(WaitForEndOfFrameCoroutine(1000));
    }

    private IEnumerator WaitForEndOfFrameCoroutine(float time)
    {
        yield return (new WaitForEndOfFrame());
        ScreenCapture.CaptureScreenshot(_screenshotPath + "/screen_" + Directory.GetFiles(_screenshotPath, "*.png").Length.ToString("D4") + ".png");
        panel.gameObject.SetActive(true);
        Application.OpenURL("file://" + _screenshotPath);
    }
}