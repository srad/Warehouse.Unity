using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    [System.Serializable]
    public class MaterialSample<T>
    {
        //public TextAsset file;
        public T sample;
        public float p;
    }

    public class Bgr
    {
        public int B;
        public int G;
        public int R;
    }

    public class Hist
    {
        public int[] B = new int[256];
        public int[] G = new int[256];
        public int[] R = new int[256];
    }

    public class HistInfo : IDistribution<Bgr>
    {
        private readonly Hist _hist;
        public float P;
        private readonly DiscreteDist<int> _distB;
        private readonly DiscreteDist<int> _distG;
        private DiscreteDist<int> _distR;

        public HistInfo(MaterialSample<Texture2D> texSampleItem)
        {
            _hist = CreateHist(texSampleItem.sample);
            P = texSampleItem.p;

            _distB = new DiscreteDist<int>(
                _hist.B
                    .Select((val, idx) => new Discrete<int> {Element = idx, P = val})
                    .ToArray());
            _distG = new DiscreteDist<int>(
                _hist.G
                    .Select((val, idx) => new Discrete<int> {Element = idx, P = val})
                    .ToArray());
            _distR = new DiscreteDist<int>(
                _hist.R
                    .Select((val, idx) => new Discrete<int> {Element = idx, P = val})
                    .ToArray());
        }

        public static Hist CreateHist(Texture2D tex)
        {
            var hist = new Hist();
            var copy = new Texture2D(tex.width, tex.height, tex.format, tex.mipmapCount, true);

            Graphics.CopyTexture(tex, copy);

            for (var y = 0; y < copy.height; y++)
            {
                var c = copy.GetPixels(0, y, copy.width, 1);
                for (var x = 0; x < c.Length; x++)
                {
                    hist.R[(int) (c[x].r * 255)] += 1;
                    hist.G[(int) (c[x].g * 255)] += 1;
                    hist.B[(int) (c[x].b * 255)] += 1;
                }
            }

            return hist;
        }

        public Bgr Sample() => new Bgr {B = _distB.Sample(), G = _distG.Sample(), R = _distR.Sample()};
    }
}