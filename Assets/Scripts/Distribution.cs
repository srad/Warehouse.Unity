using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Events
{
    public class Element<T>
    {
        public double Probability { get; set; }
        public T Item { get; set; }
    }

    /// <summary>
    /// Details please see: https://stackoverflow.com/questions/46735106/pick-random-element-from-list-with-probability
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Distribution<T> : List<Element<T>>
    {
        private List<Element<T>> _sumProbabilities = new List<Element<T>>();

        public Distribution(IEnumerable<Element<T>> elements)
        {
            foreach (var e in elements)
            {
                Add(e);
            }

            Init();
        }

        private void Init()
        {
            _sumProbabilities = new List<Element<T>>(Count);

            var sum = 0.0;

            foreach (var item in this.Take(Count - 1))
            {
                sum += item.Probability;
                _sumProbabilities.Add(new Element<T> {Probability = sum, Item = item.Item});
            }

            _sumProbabilities.Add(new Element<T> {Probability = 1.0, Item = this.Last().Item});
        }

        public T Sample()
        {
            var probability = Random.Range(0f, 1f);
            return _sumProbabilities.SkipWhile(i => i.Probability < probability).First().Item;
        }
    }
}