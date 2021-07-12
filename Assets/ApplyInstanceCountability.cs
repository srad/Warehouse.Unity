using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine;

public class ApplyInstanceCountability : MonoBehaviour
{

    public Shader segmentShader;
    public Camera segmentCamera;

    // Dictionary of Label Name -> Label Id
    Dictionary<string, int> segmentDict = new Dictionary<string, int>();

    void Start()
    {
        // Fill the Dictionary with Tag names and 'label id'
        segmentDict.Add("pallet_part", 101);

        print("Hello World");

        foreach (var entry in segmentDict)
        {
            string label = entry.Key;
            int label_id = entry.Value;

            // Object instance counters
            byte green = 0;
            byte blue = 0;

            GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(label);

            for (int i = 0; i < gameObjects.Length; i++)
            {
                var renderer = gameObjects[i].GetComponent<MeshRenderer>();
                var mpb = new MaterialPropertyBlock();

                // Reset green back to 0 for scenes with more than 255 game objects per tag
                if (green > 255)
                {
                    green = 0;
                    blue += 1;
                }

                // Create new color  with the current label id and green blue instance counters
                Color32 objColor = new Color32((byte)label_id, green, blue, 255);

                // Set the _SegmentColor variable  used by the segment shader
                mpb.SetColor("_SegmentColor", objColor);
                renderer.SetPropertyBlock(mpb);

                // Value set to 20 so color difference between objects is obvious to human eye.
                green += 20; // += 1;
            }

        }

        // Finally set the Segment shader as replacement shader on camera
        segmentCamera.SetReplacementShader(segmentShader, "RenderType");
    }
}
