using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Events
{
    public class Dist<T>
    {
        public double P { get; set; }
        public T Element { get; set; }
    }

    /// <summary>
    /// Details please see: https://stackoverflow.com/questions/46735106/pick-random-element-from-list-with-probability
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DiscreteDist<T> : List<Dist<T>>
    {
        private List<Dist<T>> _sumProbabilities = new List<Dist<T>>();

        public DiscreteDist(IEnumerable<Dist<T>> elements)
        {
            foreach (var e in elements)
            {
                Add(e);
            }

            Init();
        }

        private void Init()
        {
            _sumProbabilities = new List<Dist<T>>(Count);

            var sum = 0.0;

            foreach (var item in this.Take(Count - 1))
            {
                sum += item.P;
                _sumProbabilities.Add(new Dist<T> {P = sum, Element = item.Element});
            }

            _sumProbabilities.Add(new Dist<T> {P = 1.0, Element = this.Last().Element});
        }

        public T Sample()
        {
            var probability = Random.Range(0f, 1f);
            return _sumProbabilities.SkipWhile(i => i.P < probability).First().Element;
        }
    }
}