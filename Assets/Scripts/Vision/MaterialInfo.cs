using UnityEngine;

public class MaterialInfo
{
    public bool UseTexture { get; set; }
    public float WoodColorVariance { get; set; }
    public Color BaseColor { get; set; }
    public Texture2D Tex { get; set; }
    public bool IsDirty { get; set; }
    public Texture Dirt { get; set; }
}