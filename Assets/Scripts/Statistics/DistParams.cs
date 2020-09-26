using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class DistParams
{
    public List<Texture2D> PlankNormalMaps { get; set; }
    public List<Texture2D> DirtTextures { get; set; }
    public List<Texture2D> BrickNormalMaps { get; set; }
    public float WoodColorVariance { get; set; }
    public Material BasePalletMaterial { get; set; }
    public List<HistInfo> Hists { get; set; }
}