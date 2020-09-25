using System.Linq;
using UnityEngine;

namespace DefaultNamespace
{
    [System.Serializable]
    public class HistItem
    {
        public TextAsset file;
        public float p;
    }

    public class Bgr
    {
        public int b;
        public int g;
        public int r;
    }

    public class Hist
    {
        public float[] b;
        public float[] g;
        public float[] r;
    }

    public class HistInfo : IDistribution<Bgr>
    {
        public Hist Hist;
        public float P;
        private readonly DiscreteDist<int> _distB;
        private readonly DiscreteDist<int> _distG;
        private DiscreteDist<int> _distR;

        public HistInfo(Hist hist, float p)
        {
            Hist = hist;
            P = p;
            
            _distB = new DiscreteDist<int>(
                hist.b
                    .Select((val, idx) => new Discrete<int> {Element = idx, P = val})
                    .ToArray());
            _distG = new DiscreteDist<int>(
                hist.g
                    .Select((val, idx) => new Discrete<int> {Element = idx, P = val})
                    .ToArray());
            _distR = new DiscreteDist<int>(
                hist.r
                    .Select((val, idx) => new Discrete<int> {Element = idx, P = val})
                    .ToArray());
        }

        public Bgr Sample() => new Bgr {b = _distB.Sample(), g = _distG.Sample(), r = _distR.Sample()};
    }
}