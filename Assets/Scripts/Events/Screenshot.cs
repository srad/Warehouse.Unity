using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
  public Canvas HudCanvas;

  public void SaveScreenshot()
  {
    HudCanvas.enabled = false;
    var path = Application.dataPath;
    ScreenCapture.CaptureScreenshot(path + "/screen_" + DateTime.Now.ToShortDateString() + ".png");
    HudCanvas.enabled = true;
    Application.OpenURL("file://" + path);
  }
}
