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
    [Header("References")] public GameObject forkLift;

    [Header("Clone Objects")] public GameObject pallet;
    public GameObject shelf;

    [Header("Setup")] public bool applyDamage = true;
    public bool generateLoad = true;
    public bool missingPallets = true;

    public int zGroupSize = 6;
    public int xGroupSize = 2;
    public int yGroupCount = 3;

    public bool makeStatic = false;

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
    public float zGroupDistance = -0.8f;

    [Header("X Group")] public int xCount = 6;
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
    private Distribution<string> _distBrickRotated;
    private Distribution<string> _distBrickMissing;
    private Distribution<string> _distPlankDamage;
    private Distribution<string> _distQualityClass;
    private Distribution<int> _distBoxRotation;

    #endregion

    /// <summary>
    /// Start with some priors.
    /// </summary>
    private void DefDistributions()
    {
        // 1. Best guess (P) + add another free parameter Theta (measure) for each Distribution
        // 2. Oder aus Daten "Best Guess" extrahieren

        _distMaterial = new Distribution<Material>(new[]
        {
            new Dist<Material> {Element = material1, P = pMaterial1},
            new Dist<Material> {Element = material2, P = pMaterial2},
            new Dist<Material> {Element = material3, P = pMaterial3}
        });

        _distStackHeight = new Distribution<int>(new[]
        {
            new Dist<int> {Element = 4, P = 0.3f},
            new Dist<int> {Element = 3, P = 0.4f},
            new Dist<int> {Element = 2, P = 0.2f},
            new Dist<int> {Element = 1, P = 0.1f},
        });

        _distQualityClass = new Distribution<string>(new[]
        {
            new Dist<string> {Element = "new", P = 0.1f /*, Theta=...*/},
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

        _distBrickRotated = new Distribution<string>(new[]
        {
            new Dist<string> {Element = PalletInfo.Brick.Corner, P = 0.4f},
            new Dist<string> {Element = PalletInfo.Brick.Side, P = 0.1f},
            new Dist<string> {Element = PalletInfo.Brick.Front, P = 0.5f},
        });

        _distBrickMissing = new Distribution<string>(new[]
        {
            new Dist<string> {Element = PalletInfo.Brick.Corner, P = 0.3f},
            new Dist<string> {Element = PalletInfo.Brick.Side, P = 0.1f},
            new Dist<string> {Element = PalletInfo.Brick.Front, P = 0.6f},
        });

        _distBoxRotation = new Distribution<int>(new[]
        {
            new Dist<int> {Element = -90, P = 1 / 3f},
            new Dist<int> {Element = 90, P = 1 / 3f},
            new Dist<int> {Element = 0, P = 1 / 3f},
        });
    }

    private GameObject CreatePallet(Vector3 pos, bool load = false)
    {
        var p = Instantiate(pallet, transform.parent);
        p.transform.position = pos;
        p.tag = "pallet";

        // Basic variations
        p.transform.Rotate(Vector3.up, Random.Range(-palletRotation, palletRotation));
        p.transform.Translate(Random.Range(-xRange, xRange), 0f, 0f);
        p.transform.Translate(0f, 0f, Random.Range(-zRange, zRange));

        // Initial tags
        p.transform.Find(PalletTags.Types.PalletType).tag = PalletTags.Pallet.Type1;
        p.transform.Find(PalletTags.Types.Layers).tag = LoadInfo.NoLoad;
        p.transform.Find(PalletTags.Types.Damage).tag = PalletTags.NoDamage;
        p.transform.Find(PalletTags.Types.Load).tag = load ? PalletTags.Load : PalletTags.NoLoad;

        return p;
    }

    private CollisionProbe _probe;

    public void Start()
    {
        DefDistributions();
        Screen.SetResolution(1024, 768, false);
        _probe = forkLift.transform.Find("CollisionProbe").GetComponent<CollisionProbe>();
        Generate();
    }

    private void DestroyPallets()
    {
        var objs = GameObject.FindGameObjectsWithTag("pallet")
            .Concat(GameObject.FindGameObjectsWithTag("shelf"));

        foreach (var obj in objs)
        {
            obj.transform.parent = null;
            Destroy(obj.gameObject);
        }
    }

    public void Generate()
    {
        _probe.Clear();
        // forkLift.transform.position = _forkliftTransform.position;
        // forkLift.transform.rotation = _forkliftTransform.rotation;
        //forkLift.transform.Translate(Random.Range(-2f, 2f) * Time.deltaTime, 0, 0);

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
                for (var y = 0; y < yGroupCount; y++)
                {
                    var skipPallet = Random.Range(0f, 1f) < pPalletMissing && missingPallets;
                    if (skipPallet)
                    {
                        continue;
                    }

                    // Even when we shall load, the user defines with which probability
                    var loadPallet = generateLoad && (Random.Range(0f, 1f) < pLoaded);

                    // Random rotation
                    var rotSample = _distBoxRotation.Sample();
                    var rot = loadPallet ? rotSample + Random.Range(-5f, 5f) : 0;

                    var newPallet = CreatePallet(new Vector3(xPosition, start.y + y * 2.6f, zPosition), loadPallet);
                    newPallet.isStatic = makeStatic;

                    var mat = _distMaterial.Sample();

                    // Assign the height to the object
                    // Sample: always returns > 0
                    var height = loadPallet ? _distStackHeight.Sample() : 0;
                    newPallet.transform.Find(PalletTags.Types.Layers).tag = Convert.ToString(height);

                    // Traverse children, planks, bricks, etc.
                    var makeDamage = (pDamage > Random.Range(0f, 1f)) && applyDamage;
                    var damageCount = 0;
                    for (var j = 0; j < newPallet.transform.childCount; j++)
                    {
                        // Pallet
                        var child = newPallet.transform.GetChild(j);
                        if (child.name.StartsWith("Pallet."))
                        {
                            child.gameObject.isStatic = makeStatic;

                            // Assign material to all parts
                            var m = Instantiate(mat);
                            // Vary surface grain/texture
                            m.mainTextureOffset = new Vector2(Random.Range(-100f, 100f), Random.Range(-100f, 100f));

                            // TODO: Also take other wood normal maps (look also at images from pallets)
                            m.SetTextureOffset("_NormalMap", new Vector2(Random.Range(-50f, 50f), Random.Range(-100f, 100f)));
                            m.SetFloat("_NormalScale", Random.Range(0.1f, 1.5f));

                            child.GetComponent<Renderer>().material = m;

                            // Damage
                            if (makeDamage)
                            {
                                if (child.name.StartsWith(PalletInfo.Plank.Top) && Random.Range(0f, 1.0f) < pTopPlankMissing)
                                {
                                    Destroy(child.gameObject);
                                    damageCount++;
                                }

                                if (child.name.StartsWith(PalletInfo.Plank.Middle) && Random.Range(0f, 1.0f) < pMiddlePlankMissing)
                                {
                                    Destroy(child.gameObject);
                                    damageCount++;
                                }

                                if (child.name.StartsWith(PalletInfo.Plank.Bottom) && Random.Range(0f, 1.0f) < pBottomPlankMissing)
                                {
                                    Destroy(child.gameObject);
                                    damageCount++;
                                }

                                if (child.name.StartsWith(PalletInfo.Brick.Prefix) && Random.Range(0f, 1.0f) < pBrickMissing)
                                {
                                    var brick = _distBrickMissing.Sample();
                                    if (child.name.StartsWith(brick))
                                    {
                                        Destroy(child.gameObject);
                                        damageCount++;
                                    }
                                }

                                // Modification
                                if (child.name.StartsWith(PalletInfo.Brick.Prefix) && Random.Range(0f, 1.0f) < pRotationBrick)
                                {
                                    var brick = _distBrickRotated.Sample();
                                    if (child.name.StartsWith(brick))
                                    {
                                        child.transform.RotateAround(child.GetComponent<Renderer>().bounds.center, Vector3.up, Random.Range(-20f, 20f));
                                    }

                                    damageCount++;
                                }
                            }
                        }

                        // Box
                        if (child.name.StartsWith(PalletInfo.Box.Prefix))
                        {
                            if (loadPallet)
                            {
                                child.transform.RotateAround(child.GetComponent<Renderer>().bounds.center, Vector3.up, rot);
                                var layerHeight = int.Parse(child.tag);
                                if (layerHeight > height)
                                {
                                    Destroy(child.gameObject);
                                }

                                // Randomly toggle objects on top most layer
                                // Todo: guarantee that top most layer has at least one box (to satisfy the sample for height)
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

                    if (damageCount > 0)
                    {
                        newPallet.transform.Find(PalletTags.Types.Damage).tag = PalletTags.Damaged;
                        // Apply darkening to damaged pallets
                        for (var j = 0; j < newPallet.transform.childCount; j++)
                        {
                            var child = newPallet.transform.GetChild(j);
                            if (child.name.StartsWith("Pallet."))
                            {
                                var childMat = child.GetComponent<Renderer>().material;
                                // Linearly darken with the number of damages
                                var factor = Mathf.Max(0f, 1f - damageCount / 10f);
                                var c = new Color(childMat.color.r * factor, childMat.color.g * factor, childMat.color.b * factor);
                                mat.SetColor("_BaseColor", c);
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
        }

        shelf.SetActive(false);
        pallet.SetActive(false);
    }
}