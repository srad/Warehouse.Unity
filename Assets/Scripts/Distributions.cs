using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using Debug = UnityEngine.Debug;
using File = UnityEngine.Windows.File;

public struct DistParams
{
    public List<Texture> plankNormalMaps { get; set; }
    public List<Texture> dirtTextures { get; set; }
    public List<Texture> brickNormalMaps { get; set; }
    public List<Color> woodColors { get; set; }
    public float woodColorVariance { get; set; }
    public Material basePalletMaterial { get; set; }
    public List<HistInfo> hists { get; set; }
}

public class SurfaceInfo
{
    public Texture2D Tex { get; set; }
    public bool IsDirty { get; set; }
    public Texture Dirt { get; set; }
}

public class Distributions
{
    public DiscreteDist<DistProducer<Color>> WoodColor;
    public DiscreteDist<Texture> PlankNormalMaps;
    public DiscreteDist<Texture> BrickNormalMaps;
    public DiscreteDist<int> StackHeight;
    public DiscreteDist<string> BrickRotated;
    public DiscreteDist<string> BrickMissing;
    public DistProduceWith<string, PalletClasses> PlankDamage;
    public DiscreteDist<Texture> DirtTexture;
    public DistProduceWith<Material[], SurfaceInfo> PalletMaterial;
    public DistProduceWith<Material[], SurfaceInfo> BrickMaterial;
    public DiscreteDist<PalletClasses> PalletClass;
    public DiscreteDist<HistInfo> Hists;
    public DiscreteDist<Texture2D> PalletTexture;

    private DistParams _params;

    public Distributions(DistParams ps)
    {
        _params = ps;
        Init();
    }

    /// <summary>
    /// Start with some priors.
    /// </summary>
    private void Init()
    {
        // 1. Best guess (P) + add another free parameter Theta (measure) for each Distribution
        // 2. Oder aus Daten "Best Guess" extrahieren

        WoodColor = new DiscreteDist<DistProducer<Color>>(
            _params.woodColors
                .Select(color => new Discrete<DistProducer<Color>>
                {
                    Element = new DistProducer<Color>
                    {
                        Generator = () =>
                        {
                            // Randomly darken or brighten
                            var darkenOrBrighten = Random.Range(0f, 1f) < 0.5f ? Color.black : Color.white;
                            // Offset by a constant towards darker variances, otherwise they are typically too bright
                            return Color.Lerp(color, darkenOrBrighten, Random.Range(-_params.woodColorVariance - 0.25f, _params.woodColorVariance - 0.25f));
                        }
                    },
                    P = 1f / _params.woodColors.Count
                })
        );

        PalletMaterial = new DistProduceWith<Material[], SurfaceInfo>
        {
            Generator = surface =>
            {
                var materials = new List<Material>();
                var mat = new Material(_params.basePalletMaterial);

                mat.SetTexture("_NormalMap", PlankNormalMaps.Sample());
                mat.SetTexture("_BaseColorMap", surface.Tex);
                mat.SetFloat("_Smoothness", 0f);
                mat.SetTextureOffset("_NormalMap", new Vector2(Random.Range(-200f, 200f), Random.Range(-200f, 200f)));
                mat.SetFloat("_NormalScale", Random.Range(0.1f, 3.5f));
                mat.EnableKeyword("_NORMALMAP");
                mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                materials.Add(mat);

                if (surface.IsDirty)
                {
                    var dirtMat = new Material(_params.basePalletMaterial);
                    var tex = DirtTexture.Sample();
                    mat.SetTexture("_BaseColorMap", tex);
                    dirtMat.SetFloat("_Smoothness", 0f);
                    dirtMat.EnableKeyword("_NORMALMAP");
                    dirtMat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                    materials.Add(dirtMat);
                }

                return materials.ToArray();
            }
        };

        BrickMaterial = new DistProduceWith<Material[], SurfaceInfo>()
        {
            Generator = surface =>
            {
                var materials = new List<Material>();
                var mat = new Material(_params.basePalletMaterial);
                var brickNormalMap = BrickNormalMaps.Sample();

                mat.SetTextureScale("_BaseColorMap", new Vector2(7f, 7f));
                mat.SetTexture("_NormalMap", brickNormalMap);
                mat.SetFloat("_NormalScale", Random.Range(0.1f, 0.7f));
                mat.SetTexture("_BaseColorMap", surface.Tex);
                mat.SetFloat("_Smoothness", 0f);
                mat.SetTextureOffset("_NormalMap", new Vector2(Random.Range(-50f, 50f), Random.Range(-100f, 100f)));
                mat.SetFloat("_NormalScale", Random.Range(0.1f, 2f));
                mat.EnableKeyword("_NORMALMAP");
                mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                materials.Add(mat);

                return materials.ToArray();
            }
        };

        Hists = new DiscreteDist<HistInfo>(
            _params.hists
                .Select(hi => new Discrete<HistInfo> {Element = hi, P = hi.P})
                .ToArray());

        PalletTexture = new DiscreteDist<Texture2D>(
            _params.hists
                .Select((hi, idx) =>
                {
                    // Dynamically create texture
                    var tex = new Texture2D(400, 400, TextureFormat.ARGB32, false);

                    var hist = Hists.Find(h => h.Element == hi);
                    for (int y = 0; y < tex.height; y++)
                    {
                        for (int x = 0; x < tex.width; x++)
                        {
                            var sample = hist.Element.Sample();
                            var color = new Color(sample.r / 255f, sample.g / 255f, sample.b / 255f);
                            tex.SetPixel(x, y, color);
                        }
                    }

                    //var path = Path.Combine(Application.dataPath, "Textures", "Test", "tex_" + idx + ".png");
                    //File.WriteAllBytes(path, tex.EncodeToPNG());

                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.Apply();

                    return new Discrete<Texture2D> {Element = tex, P = hi.P};
                })
                .ToArray());

        PlankNormalMaps = new DiscreteDist<Texture>(
            _params.brickNormalMaps
                .Select(map => new Discrete<Texture> {Element = map, P = 1f / _params.plankNormalMaps.Count})
                .ToArray());

        BrickNormalMaps = new DiscreteDist<Texture>(
            _params.brickNormalMaps
                .Select(map => new Discrete<Texture> {Element = map, P = 1f / _params.brickNormalMaps.Count})
                .ToArray());

        DirtTexture = new DiscreteDist<Texture>(
            _params.dirtTextures
                .Select(tex => new Discrete<Texture> {Element = tex, P = 1f / _params.dirtTextures.Count})
                .ToArray());

        PalletClass = new DiscreteDist<PalletClasses>(new[]
        {
            new Discrete<PalletClasses> {Element = PalletClasses.New, P = .2f},
            new Discrete<PalletClasses> {Element = PalletClasses.ClassA, P = .2f},
            new Discrete<PalletClasses> {Element = PalletClasses.ClassB, P = .25f},
            new Discrete<PalletClasses> {Element = PalletClasses.ClassC, P = .25f},
            new Discrete<PalletClasses> {Element = PalletClasses.Bad, P = .1f},
        });

        StackHeight = new DiscreteDist<int>(new[]
        {
            new Discrete<int> {Element = 4, P = 0.3f},
            new Discrete<int> {Element = 3, P = 0.4f},
            new Discrete<int> {Element = 2, P = 0.2f},
            new Discrete<int> {Element = 1, P = 0.1f},
        });

        // TODO: Correct distribution would be to sample from: P(for each plank pl_i missing| given pallet class pc_i) 
        PlankDamage = new DistProduceWith<string, PalletClasses>
        {
            Generator = pc =>
            {
                var classNew = new DiscreteDist<string>(new[]
                {
                    new Discrete<string> {Element = PalletInfo.Plank.Top, P = 0.3f},
                    new Discrete<string> {Element = PalletInfo.Plank.Middle, P = 0.1f},
                    new Discrete<string> {Element = PalletInfo.Plank.Bottom, P = 0.6f},
                });
                return classNew.Sample();
            }
        };

        BrickRotated = new DiscreteDist<string>(new[]
        {
            new Discrete<string> {Element = PalletInfo.Brick.Corner, P = 0.4f},
            new Discrete<string> {Element = PalletInfo.Brick.Side, P = 0.1f},
            new Discrete<string> {Element = PalletInfo.Brick.Front, P = 0.5f},
        });

        BrickMissing = new DiscreteDist<string>(new[]
        {
            new Discrete<string> {Element = PalletInfo.Brick.Corner, P = 0.3f},
            new Discrete<string> {Element = PalletInfo.Brick.Side, P = 0.1f},
            new Discrete<string> {Element = PalletInfo.Brick.Front, P = 0.6f},
        });
    }
}