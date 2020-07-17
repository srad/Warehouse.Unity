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

    [Header("Pallet Variations")] public float palletRotation = 2f;
    public float zRange = 0.05f;
    public float xRange = 0.1f;

    #region Distributions

    private Distribution<Material> _distMaterial;
    private Distribution<int> _distStackHeight;
    private Distribution<string> _distBrickDamage;
    private Distribution<string> _distPlankDamage;
    private Distribution<string> _distQualityClass;
    private Distribution<int> _distBoxRotation;

    #endregion

    /// <summary>
    /// Start with some priors.
    /// </summary>
    private void DefDistributions()
    {
        _distMaterial = new Distribution<Material>(new[]
        {
            new Dist<Material> {Element = material1, P = pMaterial1},
            new Dist<Material> {Element = material2, P = pMaterial2},
            new Dist<Material> {Element = material3, P = pMaterial3}
        });

        _distStackHeight = new Distribution<int>(new[]
        {
            new Dist<int> {Element = 4, P = 0.2f},
            new Dist<int> {Element = 3, P = 0.4f},
            new Dist<int> {Element = 2, P = 0.2f},
            new Dist<int> {Element = 1, P = 0.1f},
            new Dist<int> {Element = 0, P = 0.1f}
        });

        _distQualityClass = new Distribution<string>(new[]
        {
            new Dist<string> {Element = "new", P = 0.1f},
            new Dist<string> {Element = "A", P = 0.35f},
            new Dist<string> {Element = "B", P = 0.35f},
            new Dist<string> {Element = "C", P = 0.1f},
            new Dist<string> {Element = "unusable", P = 0.1f},
        });

        _distPlankDamage = new Distribution<string>(new[]
        {
            new Dist<string> {Element = PalletInfo.Plank.Top, P = 0.3f},
            new Dist<string> {Element = PalletInfo.Plank.Middle, P = 0.1f},
            new Dist<string> {Element = PalletInfo.Plank.Bottom, P = 0.6f},
        });

        _distBrickDamage = new Distribution<string>(new[]
        {
            new Dist<string> {Element = PalletInfo.Brick.Corner, P = 0.1f},
            new Dist<string> {Element = PalletInfo.Brick.Side, P = 0.2f},
            new Dist<string> {Element = PalletInfo.Brick.Front, P = 0.6f},
        });

        _distBoxRotation = new Distribution<int>(new[]
        {
            new Dist<int> {Element = -90, P = 1 / 3f},
            new Dist<int> {Element = 90, P = 1 / 3f},
            new Dist<int> {Element = 0, P = 1 / 3f},
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
        DefDistributions();
        Screen.SetResolution(1024, 768, false);
        Generate();
    }

    private void DestroyPallets()
    {
        for (var i = 0; i < transform.parent.childCount; i++)
        {
            var child = transform.parent.GetChild(i);
            if (child.CompareTag("pallet") || child.CompareTag("shelf"))
            {
                // Maybe workaround, because OnTriggerExit is not executed on the CollisionProbe when Detroy() is called
                //child.transform.Translate(0, -100, 0);
                //yield return new WaitForEndOfFrame();
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
                    for (int i = 0; i < 3; i++)
                    {
                        var loadPallet = generateLoad && Random.Range(0f, 1f) < pLoaded;
                        // Random ratation
                        var rotSample = _distBoxRotation.Sample();
                        var rot = loadPallet ? rotSample + Random.Range(-5f, 5f) : 0;

                        var newPallet = CreatePallet(new Vector3(xPosition, start.y + i * 2.6f, zPosition));

                        var mat = _distMaterial.Sample();

                        // Assign the height to the object
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
                                var makeDamage = (Random.Range(0f, 1f) > pDamage) && applyDamage;
                                if (makeDamage)
                                {
                                    // Destruction
                                    if (child.name.StartsWith(PalletInfo.Plank.Top) && Random.Range(0f, 1.0f) < pTopPlankMissing)
                                    {
                                        Destroy(child.gameObject);
                                        appliedDamage = true;
                                    }

                                    else if (child.name.StartsWith(PalletInfo.Plank.Middle) && Random.Range(0f, 1.0f) < pMiddlePlankMissing)
                                    {
                                        Destroy(child.gameObject);
                                        appliedDamage = true;
                                    }

                                    else if (child.name.StartsWith(PalletInfo.Plank.Bottom) && Random.Range(0f, 1.0f) < pBottomPlankMissing)
                                    {
                                        Destroy(child.gameObject);
                                        appliedDamage = true;
                                    }

                                    else if (child.name.StartsWith(PalletInfo.Brick.Prefix) && Random.Range(0f, 1.0f) < pBrickMissing)
                                    {
                                        Destroy(child.gameObject);
                                        appliedDamage = true;
                                    }

                                    // Modification
                                    else if (child.name.StartsWith(PalletInfo.Brick.Prefix) && Random.Range(0f, 1.0f) < pBrickDamage)
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
                                    child.transform.RotateAround(child.GetChild(0).GetComponent<Renderer>().bounds.center, Vector3.up, rot);
                                    var layerHeight = int.Parse(child.tag);
                                    if (layerHeight > height)
                                    {
                                        Destroy(child.gameObject);
                                    }

                                    // Randomly toggle objects on top most layer
                                    if (layerHeight == height)
                                    {
                                        child.gameObject.SetActive(Random.Range(0f, 1f) < 0.5f);
                                    }
                                }
                                else
                                {
                                    Destroy(child.gameObject);
                                }
                            }
                        }
                    }
                }

                zPosition += zOffset;
                if (z > 0 && z % zGroupSize == 0)
                {
                    var newShelf = Instantiate(shelf, transform.parent);
                    shelf.tag = "shelf";
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