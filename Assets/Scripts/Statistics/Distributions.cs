using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;


public class Distributions
{
    public DiscreteDist<Texture2D> PlankNormalMaps;
    public DiscreteDist<Texture2D> BrickNormalMaps;
    public DiscreteDist<int> StackHeight;
    public DiscreteDist<string> BrickRotated;
    public DiscreteDist<string> BrickMissing;
    public DistProduceWith<string, PalletClasses> PlankDamageProducer;
    public DiscreteDist<Texture> DirtTexture;
    public DistProduceWith<Material[], MaterialInfo> PalletMaterialProducer;
    public DistProduceWith<Material[], MaterialInfo> BrickMaterialProducer;
    public DiscreteDist<PalletClasses> PalletClass;
    public DiscreteDist<HistInfo> Hists;
    public DiscreteDist<Color> BaseColors;
    public DistProduceWith<Texture2D, MaterialInfo> PalletTextureProducer;
    public DistProducer<Material> SurfaceDamageMaterialProducer;
    public DistProduceWith<Material, WoodMaterialInfo> WoodMaterialProducer;

    private readonly DistParams _params;

    public Distributions(DistParams ps)
    {
        _params = ps;
        Init();
    }


    public class SufraceInfo
    {
    }

    public class WoodMaterialInfo
    {
        public MaterialInfo MaterialInfo { get; set; }
        public Texture2D NormalMap { get; set; }
        public Vector4 NormalMapOffsetRange { get; set; }
        public Vector2 NormalMapScaleRange { get; set; }
        public Vector2 TextureScale { get; set; }
    }

    /// <summary>
    /// Hard-coded distributions are best guesses from domain inspection.
    /// </summary>
    private void Init()
    {
        WoodMaterialProducer = new DistProduceWith<Material, WoodMaterialInfo>
        {
            Generator = surface =>
            {
                var mat = new Material(_params.BasePalletMaterial);
                if (_params.UseTextureSamples)
                {
                    mat.SetTexture("_BaseColorMap", surface.MaterialInfo.Tex);
                    mat.SetTextureScale("_BaseColorMap", new Vector2(surface.TextureScale.x, surface.TextureScale.y));
                }
                else
                {
                    mat.SetColor("_BaseColor", surface.MaterialInfo.BaseColor);
                }

                mat.SetTexture("_NormalMap", PlankNormalMaps.Sample());
                mat.SetFloat("_Smoothness", 0f);
                mat.SetTextureOffset("_NormalMap",
                    new Vector2(Random.Range(surface.NormalMapOffsetRange.x, surface.NormalMapOffsetRange.y),
                        Random.Range(surface.NormalMapOffsetRange.z, surface.NormalMapOffsetRange.w)));
                mat.SetFloat("_NormalScale",
                    Random.Range(surface.NormalMapScaleRange.x, surface.NormalMapScaleRange.y));
                mat.EnableKeyword("_NORMALMAP");
                mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");

                return mat;
            }
        };

        SurfaceDamageMaterialProducer = new DistProducer<Material>
        {
            Generator = () =>
            {
                var mat = new Material(_params.BasePalletMaterial);
                var tex = DirtTexture.Sample();
                tex.wrapMode = TextureWrapMode.Clamp;

                mat.SetTexture("_BaseColorMap", tex);
                mat.SetFloat("_Smoothness", 0f);
                mat.EnableKeyword("_NORMALMAP");
                mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");

                // Randomize
                var tile = Random.Range(.4f, .6f);
                mat.SetTextureScale("_BaseColorMap", new Vector2(tile, tile));
                mat.SetTextureOffset("_NormalMap", new Vector2(Random.Range(-100f, 100f), Random.Range(-100f, 100f)));

                return mat;
            }
        };

        PalletMaterialProducer = new DistProduceWith<Material[], MaterialInfo>
        {
            Generator = matInfo =>
            {
                //var scale = Random.Range(60f, 100f);
                const float scale = 1f;
                var materials = new List<Material>
                {
                    WoodMaterialProducer.Sample(new WoodMaterialInfo
                    {
                        MaterialInfo = matInfo,
                        NormalMap = PlankNormalMaps.Sample(),
                        NormalMapOffsetRange = new Vector4(-200f, 200f, -200f, 200f),
                        TextureScale = new Vector2(scale, scale),
                        NormalMapScaleRange = new Vector2(1f, 5f)
                    })
                };

                // Dirt texture can be stacked on top of existing textures.
                if (matInfo.IsDirty)
                {
                    materials.Add(SurfaceDamageMaterialProducer.Sample());
                }

                return materials.ToArray();
            }
        };

        BrickMaterialProducer = new DistProduceWith<Material[], MaterialInfo>()
        {
            Generator = matInfo =>
            {
                var materials = new List<Material>
                {
                    WoodMaterialProducer.Sample(new WoodMaterialInfo
                    {
                        MaterialInfo = matInfo,
                        NormalMap = BrickNormalMaps.Sample(),
                        NormalMapOffsetRange = new Vector4(-50, 50f, -100f, 100f),
                        NormalMapScaleRange = new Vector2(0.1f, 0.7f),
                        TextureScale = new Vector2(20f, 20f),
                    })
                };

                if (matInfo.IsDirty)
                {
                    materials.Add(SurfaceDamageMaterialProducer.Sample());
                }

                return materials.ToArray();
            }
        };

        BaseColors = new DiscreteDist<Color>(
            _params.BaseColors
                .Select(c => new Discrete<Color> {Element = c.sample, P = c.p})
                .ToArray());

        Hists = new DiscreteDist<HistInfo>(
            _params.Hists
                .Select(hi => new Discrete<HistInfo> {Element = hi, P = hi.P})
                .ToArray());

        PalletTextureProducer = new DistProduceWith<Texture2D, MaterialInfo>
        {
            Generator = surface =>
            {
                // Select one histogram
                var hist = Hists.Sample();

                // Dynamically create texture
                var tex = new Texture2D(100, 100, TextureFormat.ARGB32, false);

                for (int y = 0; y < tex.height; y++)
                {
                    var colors = new Color[tex.width];
                    for (int x = 0; x < tex.width; x++)
                    {
                        // Color sampling
                        var sample = hist.Sample();
                        var var = Random.Range(surface.WoodColorVariance, 1f);
                        colors[x] = Color.Lerp(new Color(sample.R / 255f, sample.G / 255f, sample.B / 255f),
                            Color.black, var);
                    }

                    tex.SetPixels(0, y, colors.Length, 1, colors);
                }

                //var path = Path.Combine(Application.dataPath, "Textures", "Test", "tex_" + idx + ".png");
                //File.WriteAllBytes(path, tex.EncodeToPNG());

                tex.wrapMode = TextureWrapMode.Repeat;
                tex.Apply();

                return tex;
            }
        };

        PlankNormalMaps = new DiscreteDist<Texture2D>(
            _params.PlankNormalMaps
                .Select(map => new Discrete<Texture2D> {Element = map, P = 1f / _params.PlankNormalMaps.Count})
                .ToArray());

        BrickNormalMaps = new DiscreteDist<Texture2D>(
            _params.BrickNormalMaps
                .Select(map => new Discrete<Texture2D> {Element = map, P = 1f / _params.BrickNormalMaps.Count})
                .ToArray());

        DirtTexture = new DiscreteDist<Texture>(
            _params.DirtTextures
                .Select(tex => new Discrete<Texture> {Element = tex, P = 1f / _params.DirtTextures.Count})
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
        PlankDamageProducer = new DistProduceWith<string, PalletClasses>
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