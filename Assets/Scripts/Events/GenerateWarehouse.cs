using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GenerateWarehouse : MonoBehaviour
{
    [Header("References")] public GameObject forkLift;

    [Header("Clone Objects")] public GameObject pallet;
    public GameObject shelf;
    public GameObject lights;

    [Header("Setup")] public bool applyDamage = true;
    public bool generateLoad = true;
    public bool missingPallets = true;

    [Header("Material Settings")] public Material basePalletMaterial;

    public List<HistItem> histograms;

    public List<Texture> plankNormalMaps = new List<Texture>();
    public List<Texture> dirtTextures = new List<Texture>();
    public List<Texture> brickNormalMaps = new List<Texture>();
    public List<Color> woodColors = new List<Color>();
    public float woodColorVariance = 0.5f;

    [Header("General Probabilities")] [Tooltip("P(Pallet Loaded at all"), Range(0f, 1f)]
    public float pLoaded = 0.8f;

    [Tooltip("P(Pallet Damage"), Range(0f, 1f)]
    public float pDamage = 0.25f;

    [Tooltip("P(Apply Dirt To Pallet"), Range(0f, 1f)]
    public float pApplyDirt = 0.1f;

    [Tooltip("P(Pallet Missing At Space"), Range(0f, 1f)]
    public float pPalletMissing = 0.05f;

    [Tooltip("P(Brick Missing|Damage"), Range(0f, 1f)]
    public float pBrickMissing = 0.1f;

    [Tooltip("P(plank missing|damage"), Range(0f, 1f)]
    public float pTopPlankMissing = 0.01f;

    [Tooltip("P(Middle Plank Missing|Damage"), Range(0f, 1f)]
    public float pMiddlePlankMissing = 0.01f;

    [Tooltip("P(Bottom Plank Missing|Damage"), Range(0f, 1f)]
    public float pBottomPlankMissing = 0.01f;

    [Tooltip("P(Brick Damage|Damage"), Range(0f, 1f)]
    public float pBrickDamage = 0.2f;

    [Tooltip("P(Brick Rotated|Damage"), Range(0f, 1f)]
    public float pRotationBrick = 0.15f;

    [Header("Arrangement Parameters")] public float xOffset = 1.8f;

    public int xCount = 6;
    public int xGroupSize = 2;
    public float xGroupDistance = 5f;
    public float xShelfOffset = 1.85f;

    public int zCount = 18;
    public int zGroupSize = 6;
    public float zGroupDistance = -0.8f;
    public float zOffset = -1.6f;
    public float zShelfOffset = 11f;

    public int yGroupCount = 4;

    [Header("Pallet Variations")] [Range(0f, 90f)]
    public float palletRotation = 8f;

    public float zRange = 0.05f;
    public float xRange = 0.1f;

    private Distributions _dists;

    private List<GameObject> _lightRows = new List<GameObject>();

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
        _dists = new Distributions(new DistParams
        {
            hists = histograms.Select(h => new HistInfo(hist: JsonUtility.FromJson<Hist>(h.file.text), p: h.p)).ToList(),
            basePalletMaterial = basePalletMaterial,
            plankNormalMaps = plankNormalMaps,
            dirtTextures = dirtTextures,
            brickNormalMaps = brickNormalMaps,
            woodColors = woodColors,
            woodColorVariance = woodColorVariance,
        });

        Screen.SetResolution(1024, 768, false);
        _probe = forkLift.transform.Find("CollisionProbe").GetComponent<CollisionProbe>();
        Generate();
    }

    private void DestroyScene()
    {
        _probe.Clear();
        var objs = GameObject.FindGameObjectsWithTag("pallet")
            .Concat(GameObject.FindGameObjectsWithTag("shelf"))
            .Concat(GameObject.FindGameObjectsWithTag("light"));

        foreach (var obj in objs)
        {
            obj.transform.parent = null;
            Destroy(obj.gameObject);
        }
    }

    public void Generate()
    {
        DestroyScene();

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
                // Up
                for (var y = 0; y < yGroupCount; y++)
                {
                    var skipPallet = missingPallets && (Random.Range(0f, 1f) < pPalletMissing);
                    if (skipPallet)
                    {
                        continue;
                    }

                    // Even when we shall load, the user defines with which probability
                    var loadPallet = generateLoad && (Random.Range(0f, 1f) < pLoaded);

                    // Load variation: <small random rotation> + 90° rotation
                    var rot = loadPallet ? Random.Range(-5f, 5f) + (Random.Range(0f, 1f) < 0.5f ? -90 : 0) : 0;

                    var newPallet = CreatePallet(new Vector3(xPosition, start.y + y * 2.6f, zPosition), loadPallet);

                    // Assign the height to the object
                    // Sample: always returns > 0
                    var height = loadPallet ? _dists.StackHeight.Sample() : 0;
                    newPallet.transform.Find(PalletTags.Types.Layers).tag = Convert.ToString(height);

                    // Traverse children, planks, bricks, etc.
                    var makeDamage = applyDamage && (pDamage > Random.Range(0f, 1f));
                    var damageCount = 0;

                    // Each run, the brick surface structure is determined for all bricks.
                    // There are no pallet with different kind of brick.

                    var pClass = _dists.PalletClass.Sample();
                    //var dirtClass = pClass == PalletClasses.ClassB || pClass == PalletClasses.ClassC || pClass == PalletClasses.Bad;
                    var isDirty = Random.Range(0f, 1f) < pApplyDirt && Random.Range(0f, 1f) < 0.5f;
                    var tex = _dists.PalletTexture.Sample();
                    var surface = new SurfaceInfo {Tex = tex, IsDirty = isDirty};

                    for (var j = 0; j < newPallet.transform.childCount; j++)
                    {
                        var child = newPallet.transform.GetChild(j);
                        //var woodenPart = child.name.StartsWith(PalletInfo.Plank.Prefix) || child.name.StartsWith("Pallet.");

                        if (child.name.StartsWith("Pallet."))
                        {
                            var palletMaterial = _dists.PalletMaterial.Sample(surface);

                            var ren = child.GetComponent<Renderer>();
                            ren.rayTracingMode = RayTracingMode.Static;
                            ren.shadowCastingMode = ShadowCastingMode.On;

                            //if (woodenPart && isDirty)
                            //{
                            //    palletMaterial.SetTexture("_BaseColorMap", dirt);
                            //    var tile = Random.Range(.4f, .6f);
                            //    palletMaterial.SetTextureScale("_BaseColorMap", new Vector2(tile, tile));
                            //    palletMaterial.SetTextureOffset("_NormalMap", new Vector2(Random.Range(-100f, 100f), Random.Range(-100f, 100f)));
                            //}

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

                                // Brick damage
                                if (child.name.StartsWith(PalletInfo.Brick.Prefix) && Random.Range(0f, 1.0f) < pBrickDamage)
                                {
                                    if (Random.Range(0f, 1.0f) < pBrickMissing && child.name.StartsWith(_dists.BrickMissing.Sample()))
                                    {
                                        Destroy(child.gameObject);
                                        damageCount++;
                                    }
                                    else if (child.name.StartsWith(PalletInfo.Brick.Prefix) && Random.Range(0f, 1.0f) < pRotationBrick && child.name.StartsWith(_dists.BrickRotated.Sample()))
                                    {
                                        child.transform.RotateAround(child.GetComponent<Renderer>().bounds.center, Vector3.up, Random.Range(-20f, 20f));
                                        damageCount++;
                                    }
                                }
                            }

                            // Material assignment
                            if (child.name.StartsWith(PalletInfo.Plank.Prefix))
                            {
                                ren.materials = palletMaterial;
                            }
                            else if (child.name.StartsWith(PalletInfo.Brick.Prefix))
                            {
                                var brickMaterial = _dists.BrickMaterial.Sample(surface);
                                ren.materials = brickMaterial;
                            }
                        }
                        else if (child.name.StartsWith(PalletInfo.Box.Prefix))
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
                        // TODO: Move to correct position in the distribution sampler
                        /*
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
                                material.SetColor("_BaseColor", c);
                            }
                        }
                        */
                    }
                }

                zPosition += zOffset;
                if (z > 0 && z % (zGroupSize + 1) == 0)
                {
                    var newShelf = Instantiate(shelf, transform.parent);
                    shelf.tag = "shelf";
                    newShelf.transform.position = new Vector3(xPositionShelf, shelf.transform.position.y, zPositionShelf);

                    zPositionShelf -= zShelfOffset;
                    zPosition += zGroupDistance;

                    if (x % 2 == 0)
                    {
                        var lightRow = Instantiate(lights, lights.transform, lights.transform.parent);
                        lightRow.transform.Translate(xOffset * x, 0, 0, Space.Self);
                        lightRow.tag = "light";
                    }
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

        //shelf.SetActive(false);
        pallet.SetActive(false);
    }
}