using System.Collections;
using System.Collections.Generic;
using Events;
using UnityEditor;
using UnityEngine;

public class GenerateWarehouse : MonoBehaviour
{
    [Header("Clone Objects")] public GameObject pallet;
    public GameObject shelf;

    [Header("Material Settings")] public float pMaterial1 = 0.7f;
    public float pMaterial2 = 0.2f;
    public float pMaterial3 = 0.1f;

    public Material material1;
    public Material material2;
    public Material material3;

    [Header("Distance between each object")]
    public float xOffset = 1.8f;

    public float zOffset = -1.6f;

    public float zShelfOffset = 11f;
    public float xShelfOffset = 1.85f;

    [Header("Z Group")] public int zCount = 18;
    public int zGroupSize = 6;
    public float zGroupDistance = -0.8f;

    [Header("X Group")] public int xCount = 6;
    public int xGroupSize = 2;
    public float xGroupDistance = 5f;

    [Header("General Probabilities")] public float pLoaded = 0.8f;
    public float pDamage = 0.25f;

    [Header("Damage Probabilities")] public float pBrickMissing = 0.1f;

    public float pTopPlankMissing = 0.01f;
    public float pMiddlePlankMissing = 0.01f;
    public float pBottomPlankMissing = 0.01f;

    public float pRotationBrick = 0.15f;
    public float pRotationPallet = 0.1f;

    [Header("Damage Ranges")] public float palletRotation = 2f;
    public float zRange = 0.05f;
    public float xRange = 0.1f;

    private Distribution<Material> _matDist;
    private Distribution<int> _heightDist;

    private void InitMaterialProbabilities()
    {
        _matDist = new Distribution<Material>()
        {
            new Element<Material> {Item = material1, Probability = pMaterial1},
            new Element<Material> {Item = material2, Probability = pMaterial2},
            new Element<Material> {Item = material3, Probability = pMaterial3}
        };
        _matDist.Init();

        _heightDist = new Distribution<int>()
        {
            new Element<int> {Item = 4, Probability = 0.5f},
            new Element<int> {Item = 3, Probability = 0.3f},
            new Element<int> {Item = 2, Probability = 0.1f},
            new Element<int> {Item = 1, Probability = 0.1f}
        };
        _heightDist.Init();
    }

    private int BoxToLayer(string name)
    {
        if (name.StartsWith("Box.Layer1"))
        {
            return 1;
        }

        if (name.StartsWith("Box.Layer2"))
        {
            return 2;
        }

        if (name.StartsWith("Box.Layer3"))
        {
            return 3;
        }

        return 3;
    }

    public void Start()
    {
        InitMaterialProbabilities();

        var start = pallet.transform.position;
        var xPosition = start.x;
        var xPositionShelf = shelf.transform.position.x;

        // x direction
        for (var x = 0; x < xCount; x++)
        {
            var zPosition = start.z;
            var zPositionShelf = shelf.transform.position.z;

            // -z Direction
            for (var z = 0; z < zCount; z++)
            {
                var loadPallet = Random.Range(0f, 1f) < pLoaded;
                var newPallet = Instantiate(pallet);
                newPallet.transform.position = new Vector3(xPosition, start.y, zPosition);
                newPallet.transform.Rotate(Vector3.up, Random.Range(-palletRotation, palletRotation));
                newPallet.transform.Translate(Random.Range(-xRange, xRange), 0f, 0f);
                newPallet.transform.Translate(0f, 0f, Random.Range(-zRange, zRange));

                var mat = _matDist.Sample();
                var height = _heightDist.Sample();

                for (var j = 0; j < newPallet.transform.childCount; j++)
                {
                    // Pallet
                    var child = newPallet.transform.GetChild(j);
                    if (child.name.StartsWith("Pallet"))
                    {
                        child.GetComponent<Renderer>().material = mat;

                        // Damage
                        if (Random.Range(0f, 1f) < pDamage)
                        {
                            if (child.name.StartsWith("Pallet.Plank.Top"))
                            {
                                child.gameObject.SetActive(Random.Range(0f, 1.0f) > pTopPlankMissing);
                            }

                            if (child.name.StartsWith("Pallet.Plank.Middle"))
                            {
                                child.gameObject.SetActive(Random.Range(0f, 1.0f) > pMiddlePlankMissing);
                            }

                            if (child.name.StartsWith("Pallet.Plank.Bottom"))
                            {
                                child.gameObject.SetActive(Random.Range(0f, 1.0f) > pBottomPlankMissing);
                            }

                            if (child.name.StartsWith("Pallet.Brick"))
                            {
                                child.gameObject.SetActive(Random.Range(0f, 1.0f) < pBrickMissing);
                            }

                            if (child.name.StartsWith("Pallet.Brick"))
                            {
                                if (Random.Range(0f, 1f) < pRotationBrick)
                                {
                                    child.transform.RotateAround(child.GetComponent<Renderer>().bounds.center, Vector3.up, Random.Range(-10f, 10f));
                                }
                            }
                        }
                    }

                    // Box
                    if (child.name.StartsWith("Box.Layer"))
                    {
                        if (loadPallet)
                        {
                            var layerHeight = BoxToLayer(child.name);
                            if (layerHeight > height)
                            {
                                child.gameObject.SetActive(false);
                            }

                            // Randomly toggle objects on top most layer
                            if (layerHeight >= height)
                            {
                                child.gameObject.SetActive(Random.Range(0f, 1f) < 0.7f);
                            }
                        }
                        else
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }

                zPosition += zOffset;
                if (z > 0 && z % zGroupSize == 0)
                {
                    var newShelf = Instantiate(shelf);
                    newShelf.transform.position = new Vector3(xPositionShelf, shelf.transform.position.y, zPositionShelf);

                    zPositionShelf -= zShelfOffset;
                    zPosition += zGroupDistance;
                }
            }

            xPosition += xOffset;
            xPositionShelf += xShelfOffset;
            if (x % xGroupSize == 0)
            {
                xPositionShelf += xGroupDistance;
                xPosition += xGroupDistance;
            }
        }

        // Remove the template GameObjects
        shelf.SetActive(false);
        pallet.SetActive(false);
    }
}