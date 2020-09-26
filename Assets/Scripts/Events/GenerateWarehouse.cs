using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
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

    public List<TexSampleItem> textureSamples;

    public List<Texture2D> plankNormalMaps = new List<Texture2D>();
    public List<Texture2D> dirtTextures = new List<Texture2D>();
    public List<Texture2D> brickNormalMaps = new List<Texture2D>();
    [Range(0f, 1f)] public float woodColorVariance = 0.5f;

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
    //public int zGroupSize = 6;
    //public float zGroupDistance = -0.8f;
    public float zOffset = -1.6f;
    //public float zShelfOffset = 11f;

    public int xLightCount = 10;
    public float xLightDistance = 6f;

    public int yGroupCount = 4;

    [Header("Pallet Variations")] [Range(0f, 90f)]
    public float palletRotation = 8f;

    public float zRange = 0.05f;
    public float xRange = 0.1f;

    private Distributions _dists;

    private GameObject CreatePallet(Vector3 pos, bool load = false)
    {
        // Add uncertainty
        pos.x += Random.Range(-xRange, xRange);
        pos.z += Random.Range(-zRange, zRange);

        var p = Instantiate(pallet, pallet.transform.parent);
        p.transform.Translate(pos, Space.World);
        p.tag = "pallet";

        // Basic variations
        p.transform.Rotate(Vector3.up, Random.Range(-palletRotation, palletRotation));

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
            Hists = textureSamples.Select(h => new HistInfo(h)).ToList(),
            //Hists = histograms.Select(h => new HistInfo(hist: JsonUtility.FromJson<Hist>(h.file.text), p: h.p)).ToList(),
            BasePalletMaterial = basePalletMaterial,
            PlankNormalMaps = plankNormalMaps,
            DirtTextures = dirtTextures,
            BrickNormalMaps = brickNormalMaps,
            WoodColorVariance = woodColorVariance,
        });

        Screen.SetResolution(1024, 768, false);
        _probe = forkLift.transform.Find("CollisionProbe").GetComponent<CollisionProbe>();
        Generate();
    }

    /// <summary>
    /// This method only destroy the dynamically generated game object, not the foundation of the warehouse.
    /// </summary>
    private void DestroyScene()
    {
        _probe.Clear();
        var objs = GameObject.FindGameObjectsWithTag("pallet")
            .Concat(GameObject.FindGameObjectsWithTag("shelf"));

        foreach (var obj in objs)
        {
            obj.transform.parent = null;
            Destroy(obj.gameObject);
        }
    }

    private void AddLights()
    {
        for (var x = 1; x <= xLightCount; x++)
        {
            var lightRow = Instantiate(lights, lights.transform, lights.transform.parent);
            lightRow.transform.Translate(xLightDistance * x, 0, 0, Space.World);
            lightRow.tag = "light";
        }
    }

    public void Generate()
    {
        DestroyScene();
        
        //AddLights();

        shelf.SetActive(true);
        pallet.SetActive(true);

        //var start = pallet.transform.position;
        //var xPositionShelf = shelf.transform.position.x;

        // x direction
        var gap = 0f;
        for (var x = 0; x < xCount; x++)
        {
            if (x == 1 || (x > 1 && x % xGroupSize == 1))
            {
                gap += xGroupDistance;
            }

            var xPosition = x * xShelfOffset + gap;

            var newShelf = Instantiate(shelf, shelf.transform.parent);
            shelf.tag = "shelf";
            newShelf.transform.Translate(xPosition, 0, 0, Space.World);

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

                    var newPallet = CreatePallet(new Vector3(xPosition, y * 2.6f, -z * zOffset), loadPallet);

                    // Assign the height to the object
                    // Sample: always returns > 0
                    var height = loadPallet ? _dists.StackHeight.Sample() : 0;
                    newPallet.transform.Find(PalletTags.Types.Layers).tag = Convert.ToString(height);

                    // Traverse children, planks, bricks, etc.
                    var makeDamage = applyDamage && (pDamage > Random.Range(0f, 1f));
                    var damageCount = 0;

                    // Each run, the brick surface structure is determined for all bricks.
                    // There are no pallet with different kind of brick.

                    //var pClass = _dists.PalletClass.Sample();
                    //var dirtClass = pClass == PalletClasses.ClassB || pClass == PalletClasses.ClassC || pClass == PalletClasses.Bad;
                    var isDirty = Random.Range(0f, 1f) < pApplyDirt && Random.Range(0f, 1f) < 0.5f;
                    var surface = new MaterialInfo {WoodColorVariance = woodColorVariance};
                    var tex = _dists.PalletTextureProducer.Sample(surface);
                    surface.Tex = tex;
                    surface.IsDirty = isDirty;

                    for (var childIdx = 0; childIdx < newPallet.transform.childCount; childIdx++)
                    {
                        var child = newPallet.transform.GetChild(childIdx);
                        //var woodenPart = child.name.StartsWith(PalletInfo.Plank.Prefix) || child.name.StartsWith("Pallet.");

                        if (child.name.StartsWith("Pallet."))
                        {
                            var palletMaterial = _dists.PalletMaterialProducer.Sample(surface);

                            var ren = child.GetComponent<Renderer>();
                            ren.rayTracingMode = RayTracingMode.Static;
                            ren.shadowCastingMode = ShadowCastingMode.On;

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
                                var brickMaterial = _dists.BrickMaterialProducer.Sample(surface);
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
                    }
                }
            }
        }

        shelf.SetActive(false);
        pallet.SetActive(false);
    }
}