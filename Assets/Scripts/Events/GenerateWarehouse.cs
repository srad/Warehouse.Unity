using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GenerateWarehouse : MonoBehaviour
{
    public GameObject pallet;
    public float xOffset;
    public float zOffset = -1.4f;
    public float yOffset;
    public Material material1;
    public float randomness = 0.1f;

    public int zCount = 20;
    public int zGroupSize = 5;
    public float zGroupDistance = -.4f;

    public int xCount = 2;
    public int xGroupSize = 2;
    public float xGroupDistance = 3f;

    public void Start()
    {
        var start = pallet.transform.position;
        var xPosition = start.x;

        // x direction
        for (var x = 0; x < xCount; x++)
        {
            var zPosition = start.z;
            // -z Direction
            for (var z = 0; z < zCount; z++)
            {
                var newInstance = Instantiate(pallet);
                newInstance.transform.position = new Vector3(xPosition, start.y, zPosition);
                for (var j = 0; j < newInstance.transform.childCount; j++)
                {
                    var child = newInstance.transform.GetChild(z);
                    if (child.name.StartsWith("Pallet.Plank.Top"))
                    {
                        Debug.Log($"{z}/{j}: {child.name}");
                    }
                }

                zPosition += zOffset;
                if (z > 0 && z % zGroupSize == 0)
                {
                    zPosition += zGroupDistance;
                }
            }

            xPosition += xOffset;
            if (x % xGroupSize == 0)
            {
                xPosition += xGroupDistance;
            }
        }

        pallet.SetActive(false);
    }
}