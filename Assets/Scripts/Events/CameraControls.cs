using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public Camera camera;

    public void FOVMore()
    {
        camera.fieldOfView += 1;
    }

    public void FOVLess()
    {
        camera.fieldOfView -= 1;
    }

    public void RotUp()
    {
        camera.transform.Rotate(camera.transform.rotation.x - 1, 0, 0);
    }

    public void RotDown()
    {
        camera.transform.Rotate(camera.transform.rotation.x + 1, 0, 0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            RotUp();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            RotDown();
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            FOVMore();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            FOVLess();
        }
    }
}