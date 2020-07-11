﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class GenerateWarehouse : MonoBehaviour
{
    private class Prob<T>
    {
        public double Probability { get; set; }
        public T Item { get; set; }
    }

    [Header("Clone Settings")] public GameObject pallet;
    public GameObject shelf;

    [Header("Material Settings")] public float pMaterial1 = 0.7f;
    public float pMaterial2 = 0.2f;
    public float pMaterial3 = 0.1f;

    public Material material1;
    public Material material2;
    public Material material3;

    [Header("Distance between each gameobject")]
    public float xOffset;

    public float zOffset = -1.4f;
    public float yOffset;

    public float zShelfOffset = 10f;

    [Header("Z Info")] public int zCount = 20;
    public int zGroupSize = 5;
    public float zGroupDistance = -.4f;

    [Header("X Info")] public int xCount = 2;
    public int xGroupSize = 2;
    public float xGroupDistance = 3f;

    [Header("Probability Intervals")] public float palletRotation = 3f;
    public float zRange = 0.2f;
    public float xRange = 0.2f;

    [Header("Probabilities")] public float pDamage = 0.2f;
    public float pBrickMissing = 0.1f;

    public float pTopPlankMissing = 0.01f;
    public float pMiddlePlankMissing = 0.01f;
    public float pBottomPlankMissing = 0.01f;

    public float pRotationBrick = 0.1f;
    public float pRotationPallet = 0.1f;

    private float Rand() => Random.Range(-1.0f, 1.0f);

    private List<Prob<Material>> _materialProbability;
    private List<Prob<Material>> _sumProbabilities;

    /// <summary>
    /// See https://stackoverflow.com/questions/46735106/pick-random-element-from-list-with-probability
    /// </summary>
    private void InitMaterialProbabilities()
    {
        _materialProbability = new List<Prob<Material>>()
        {
            new Prob<Material> {Item = material1, Probability = pMaterial1},
            new Prob<Material> {Item = material2, Probability = pMaterial2},
            new Prob<Material> {Item = material3, Probability = pMaterial3}
        };

        _sumProbabilities = new List<Prob<Material>>(_materialProbability.Count);

        var sum = 0.0;

        foreach (var item in _materialProbability.Take(_materialProbability.Count - 1))
        {
            sum += item.Probability;
            _sumProbabilities.Add(new Prob<Material> {Probability = sum, Item = item.Item});
        }

        _sumProbabilities.Add(new Prob<Material> {Probability = 1.0, Item = _materialProbability.Last().Item});
    }

    private Material PickRandomMaterial()
    {
        var probability = Random.Range(0f, 1f);
        return _sumProbabilities.SkipWhile(i => i.Probability < probability).First().Item;
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
                var newInstance = Instantiate(pallet);
                newInstance.transform.position = new Vector3(xPosition, start.y, zPosition);
                newInstance.transform.Rotate(Vector3.up, Random.Range(-palletRotation, palletRotation));
                newInstance.transform.Translate(Random.Range(-xRange, xRange), 0f, 0f);
                newInstance.transform.Translate(0f, 0f, Random.Range(-zRange, zRange));

                var mat = PickRandomMaterial();

                for (var j = 0; j < newInstance.transform.childCount; j++)
                {
                    // Material
                    var child = newInstance.transform.GetChild(j);
                    if (child.name.StartsWith("Pallet"))
                    {
                        child.GetComponent<Renderer>().material = mat;
                    }

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
            xPositionShelf += xOffset;
            if (x % xGroupSize == 0)
            {
                xPositionShelf += xGroupDistance;
                xPosition += xGroupDistance;
            }
        }

        shelf.SetActive(false);
        pallet.SetActive(false);
    }
}