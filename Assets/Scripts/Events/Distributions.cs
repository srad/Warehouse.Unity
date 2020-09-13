using System.Linq;
using DefaultNamespace;
using Events;
using UnityEngine;

public class Distributions
{
    public DiscreteDist<DistProducer<Color>> WoodColor;
    public DiscreteDist<Texture> PlankNormalMaps;
    public DiscreteDist<Texture> BrickNormalMaps;
    public DiscreteDist<int> StackHeight;
    public DiscreteDist<string> BrickRotated;
    public DiscreteDist<string> BrickMissing;
    public DiscreteDist<string> PlankDamage;
    public DiscreteDist<Texture> DirtTexture;
    public DistProduceWith<Material, Color> PalletMaterial;
    public DistProduceWith<Material, Color> BrickMaterial;
    public DiscreteDist<PalletClass> PalletClass;

    private DistParams _params;

    public Distributions(DistParams ps)
    {
        _params = ps;
    }

    /// <summary>
    /// Start with some priors.
    /// </summary>
    private void DefDistributions()
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
                            return Color.Lerp(color, darkenOrBrighten, Random.Range(-_params.woodColorVariance, _params.woodColorVariance));
                        }
                    },
                    P = 1f / _params.woodColors.Count
                })
        );

        PalletMaterial = new DistProduceWith<Material, Color>
        {
            Generator = color =>
            {
                var mat = new Material(Shader.Find("HDRP/Lit"));

                mat.SetTexture("_NormalMap", PlankNormalMaps.Sample());
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", 0f);
                mat.SetTextureOffset("_NormalMap", new Vector2(Random.Range(-100f, 100f), Random.Range(-100f, 100f)));
                mat.SetFloat("_NormalScale", Random.Range(0.5f, 3.5f));

                return mat;
            }
        };

        BrickMaterial = new DistProduceWith<Material, Color>()
        {
            Generator = color =>
            {
                var mat = new Material(Shader.Find("HDRP/Lit"));
                var brickNormalMap = BrickNormalMaps.Sample();

                mat.SetTextureScale("_BaseColorMap", new Vector2(7f, 7f));
                mat.SetTexture("_NormalMap", brickNormalMap);
                mat.SetFloat("_NormalScale", Random.Range(0.1f, 0.7f));
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", 0f);
                mat.SetTextureOffset("_NormalMap", new Vector2(Random.Range(-50f, 50f), Random.Range(-100f, 100f)));
                mat.SetFloat("_NormalScale", Random.Range(0.1f, 1.5f));

                return mat;
            }
        };

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

        PalletClass = new DiscreteDist<PalletClass>(new[]
        {
            new Discrete<PalletClass> {Element = global::PalletClass.New, P = .2f},
            new Discrete<PalletClass> {Element = global::PalletClass.New, P = .2f},
            new Discrete<PalletClass> {Element = global::PalletClass.New, P = .25f},
            new Discrete<PalletClass> {Element = global::PalletClass.New, P = .25f},
            new Discrete<PalletClass> {Element = global::PalletClass.Bad, P = .1f},
        });

        StackHeight = new DiscreteDist<int>(new[]
        {
            new Discrete<int> {Element = 4, P = 0.3f},
            new Discrete<int> {Element = 3, P = 0.4f},
            new Discrete<int> {Element = 2, P = 0.2f},
            new Discrete<int> {Element = 1, P = 0.1f},
        });

        PlankDamage = new DiscreteDist<string>(new[]
        {
            new Discrete<string> {Element = PalletInfo.Plank.Top, P = 0.3f},
            new Discrete<string> {Element = PalletInfo.Plank.Middle, P = 0.1f},
            new Discrete<string> {Element = PalletInfo.Plank.Bottom, P = 0.6f},
        });

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