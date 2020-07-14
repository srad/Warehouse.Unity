using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Events;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateWarehouse : MonoBehaviour
{
    [Header("Clone Objects")] public GameObject pallet;
    public GameObject shelf;

    [Header("Setup")] public bool applyDamage = true;
    public bool generateLoad = true;
    public bool missingPallets = true;

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
    public float pPalletMissing = 0.05f;

    [Header("Damage Probabilities")] public float pBrickMissing = 0.1f;
    public float pTopPlankMissing = 0.01f;
    public float pMiddlePlankMissing = 0.01f;
    public float pBottomPlankMissing = 0.01f;
    public float pBrickDamage = 0.2f;

    public float pRotationBrick = 0.15f;

    [Header("Damage Ranges")] public float palletRotation = 2f;
    public float zRange = 0.05f;
    public float xRange = 0.1f;

    #region Distributions

    private Distribution<Material> _distMaterial;
    private Distribution<int> _distStackHeight;
    private Distribution<string> _distBrickDamage;

    #endregion

    private void InitMaterialProbabilities()
    {
        _distMaterial = new Distribution<Material>(new[]
        {
            new Element<Material> {Item = material1, Probability = pMaterial1},
            new Element<Material> {Item = material2, Probability = pMaterial2},
            new Element<Material> {Item = material3, Probability = pMaterial3}
        });

        _distStackHeight = new Distribution<int>(new[]
        {
            new Element<int> {Item = 3, Probability = 0.5f},
            new Element<int> {Item = 2, Probability = 0.3f},
            new Element<int> {Item = 1, Probability = 0.1f},
            new Element<int> {Item = 0, Probability = 0.1f}
        });

        _distBrickDamage = new Distribution<string>(new[]
        {
            new Element<string> {Item = PalletInfo.Brick.Corner, Probability = 0.1f},
            new Element<string> {Item = PalletInfo.Brick.Side, Probability = 0.2f},
            new Element<string> {Item = PalletInfo.Brick.Front, Probability = 0.6f}
        });
    }

    private GameObject CreatePallet(Vector3 pos)
    {
        var newPallet = Instantiate(pallet, transform.parent);
        newPallet.transform.position = pos;
        newPallet.tag = "pallet";

        // Basic variations
        newPallet.transform.Rotate(Vector3.up, Random.Range(-palletRotation, palletRotation));
        newPallet.transform.Translate(Random.Range(-xRange, xRange), 0f, 0f);
        newPallet.transform.Translate(0f, 0f, Random.Range(-zRange, zRange));

        // Initial tags
        newPallet.transform.Find(PalletTags.Types.PalletType).tag = PalletTags.Pallet.Type1;
        newPallet.transform.Find(PalletTags.Types.Layers).tag = LoadInfo.NoLoad;
        newPallet.transform.Find(PalletTags.Types.Damage).tag = PalletTags.NoDamage;

        return newPallet;
    }

    public void Start()
    {
        InitMaterialProbabilities();
        Generate();
    }

    private void DestroyPallets()
    {
        GameObject.Find("CollisionProbe").GetComponent<CollisionProbe>().CollidedPallets.Clear();
        for (var i = 0; i < transform.parent.childCount; i++)
        {
            var child = transform.parent.GetChild(i);
            if (child.CompareTag("pallet"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void Generate()
    {
        DestroyPallets();

        shelf.SetActive(true);
        pallet.SetActive(true);

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
                var skipPallet = Random.Range(0f, 1f) < pPalletMissing && missingPallets;
                if (!skipPallet)
                {
                    var loadPallet = generateLoad && Random.Range(0f, 1f) < pLoaded;
                    var newPallet = CreatePallet(new Vector3(xPosition, start.y, zPosition));
                    var mat = _distMaterial.Sample();
                    var height = generateLoad ? _distStackHeight.Sample() : 0;
                    newPallet.transform.Find(PalletTags.Types.Layers).tag = Convert.ToString(height);

                    // Traverse children, planks, bricks, etc.
                    for (var j = 0; j < newPallet.transform.childCount; j++)
                    {
                        // Pallet
                        var child = newPallet.transform.GetChild(j);
                        if (child.name.StartsWith("Pallet."))
                        {
                            // Assign material to all parts
                            var m = Instantiate(mat);
                            // Vary surface grain/texture
                            m.mainTextureOffset = new Vector2(Random.Range(-100f, 100f), Random.Range(-50f, 50f));
                            child.GetComponent<Renderer>().material = m;

                            var appliedDamage = false;
                            // Damage
                            var makeDamage = (Random.Range(0f, 1f) < pDamage) && applyDamage;
                            if (makeDamage)
                            {
                                if (child.name.StartsWith(PalletInfo.Plank.Top))
                                {
                                    child.gameObject.SetActive(Random.Range(0f, 1.0f) < pTopPlankMissing);
                                    appliedDamage = true;
                                }

                                if (child.name.StartsWith(PalletInfo.Plank.Middle))
                                {
                                    child.gameObject.SetActive(Random.Range(0f, 1.0f) < pMiddlePlankMissing);
                                    appliedDamage = true;
                                }

                                if (child.name.StartsWith(PalletInfo.Plank.Bottom))
                                {
                                    child.gameObject.SetActive(Random.Range(0f, 1.0f) < pBottomPlankMissing);
                                    appliedDamage = true;
                                }

                                if (child.name.StartsWith(PalletInfo.Brick.Prefix))
                                {
                                    child.gameObject.SetActive(Random.Range(0f, 1.0f) < pBrickMissing);
                                    appliedDamage = true;
                                }

                                if (child.name.StartsWith(PalletInfo.Brick.Prefix) && Random.Range(0f, 1.0f) < pBrickDamage)
                                {
                                    var brick = _distBrickDamage.Sample();
                                    if (child.name.StartsWith(brick))
                                    {
                                        child.transform.RotateAround(child.GetComponent<Renderer>().bounds.center, Vector3.up, Random.Range(-20f, 20f));
                                        appliedDamage = true;
                                    }
                                }

                                if (appliedDamage)
                                {
                                    newPallet.transform.Find(PalletTags.Types.Damage).tag = PalletTags.Damaged;
                                }
                            }
                        }

                        // Box
                        if (child.name.StartsWith(PalletInfo.Box.Prefix))
                        {
                            if (loadPallet)
                            {
                                child.transform.RotateAround(child.GetChild(0).GetComponent<Renderer>().bounds.center, Vector3.up, Random.Range(-5f, 5f));
                                var layerHeight = PalletInfo.Box.ToInt(child.name);
                                if (layerHeight >= height)
                                {
                                    child.gameObject.SetActive(false);
                                }

                                // Randomly toggle objects on top most layer
                                if (layerHeight == height)
                                {
                                    child.gameObject.SetActive(Random.Range(0f, 1f) < 0.5f);
                                }
                            }
                            else
                            {
                                child.gameObject.SetActive(false);
                            }
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

            shelf.SetActive(false);
            pallet.SetActive(false);
        }
    }
}