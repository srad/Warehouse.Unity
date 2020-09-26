using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class Discrete<T>
{
    public T Element { get; set; }
    public double P { get; set; }
}

public interface IDistribution<out T>
{
    T Sample();
}

public interface IDistributionWith<out T, in TU>
{
    T Sample(TU with);
}

/// <summary>
/// Details please see: https://stackoverflow.com/questions/46735106/pick-random-element-from-list-with-probability
/// </summary>
/// <typeparam name="T"></typeparam>
public class DiscreteDist<T> : List<Discrete<T>>, IDistribution<T>
{
    private List<Discrete<T>> _sumProbabilities = new List<Discrete<T>>();
    private double _lower = 0f;
    private double _upper = 1f;

    public DiscreteDist(IEnumerable<Discrete<T>> elements)
    {
        foreach (var e in elements)
        {
            Add(e);
        }

        Init();
    }

    private void Init()
    {
        _sumProbabilities = new List<Discrete<T>>(Count);

        var sum = 0.0;
        _lower = 0f;

        foreach (var item in this.Take(Count - 1))
        {
            sum += item.P;
            _sumProbabilities.Add(new Discrete<T> {P = sum, Element = item.Element});
        }

        _upper = sum;

        _sumProbabilities.Add(new Discrete<T> {P = 1.0, Element = this.Last().Element});
    }

    public T Sample()
    {
        var probability = Random.Range((float) _lower, (float) _upper);
        return _sumProbabilities.SkipWhile(i => i.P < probability).First().Element;
    }
}

public class DistProducer<T> : IDistribution<T>
{
    public Func<T> Generator;
    public T Sample() => Generator();
}

public class DistProduceWith<T, TU> : IDistributionWith<T, TU>
{
    public Func<TU, T> Generator;
    public T Sample(TU some) => Generator(some);
}