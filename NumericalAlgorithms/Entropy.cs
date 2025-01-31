﻿using FftSharp.Windows;
using Microsoft.VisualBasic.ApplicationServices;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.Styles;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics;
using static ScottPlot.Plottable.PopulationPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SignalAnalysis;

public enum EntropyMethod
{
    BruteForce,
    MonteCarloUniform,
    MonteCarloRandom,
    MonteCarloRandomSorted
}

/// <summary>
/// Routines to compute the entropy in signals (data-array points)
/// <seealso cref="https://www.codeproject.com/Articles/27030/Approximate-and-Sample-Entropies-Complexity-Metric"/>
/// <seealso cref="https://pdfs.semanticscholar.org/6841/efb4c40a74c1faa9f36e6d949d1ee330bfa9.pdf"/>
/// <seealso cref="https://en.wikipedia.org/wiki/Sample_entropy"/>
/// </summary>
public static class Complexity
{
    [Obsolete("This is deprecated. Use Entropy_Parallel instead with is roughly twice as faster")]
    /// <summary>
    /// Computes the approximate and sample entropies of a physiological time-series signals (typically used to diagnose diseased states).
    /// ApEn reflects the likelihood that similar patterns of observations will not be followed by additional similar observations. A time series containing many repetitive patterns has a relatively small ApEn; a less predictable process has a higher ApEn.
    /// A smaller value of SampEn also indicates more self-similarity in data set or less noise.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="dim">Embedding dimension</param>
    /// <param name="fTol">Factor to compute the tolerance so that the total is typically equal to 0.2*std</param>
    /// <param name="std">Standard deviation of the population</param>
    /// <returns>AppEn and SampEn</returns>
    public static (double AppEn, double SampEn) Entropy(double[] data, CancellationToken ct, uint dim = 2, double fTol = 0.2, double? std = null)
    {
        long upper = data.Length - (dim + 1) + 1;
        bool isEqual;
        ulong AppEn_Cum, AppEn_Cum1;
        ulong SampEn_Cum = 0, SampEn_Cum1 = 0;
        double sum = 0.0;
        double appEn, sampEn;
        double tolerance;
        if (std.HasValue)
            tolerance = std.Value * fTol;
        else
            tolerance = StdDev<double>(data) * fTol;


        for (uint i = 0; i < upper; i++)
        {
            AppEn_Cum = 0;
            AppEn_Cum1 = 0;
            for (uint j = 0; j < upper; j++)
            {
                isEqual = true;
                //m - length series
                for (uint k = 0; k < dim; k++)
                {
                    if (Math.Abs(data[i + k] - data[j + k]) > tolerance)
                    {
                        isEqual = false;
                        break;
                    }
                    if (ct.IsCancellationRequested)
                        throw new OperationCanceledException("CancelEntropy", ct);
                }
                if (isEqual)
                {
                    AppEn_Cum++;
                    SampEn_Cum++;
                }

                //m+1 - length series
                if (isEqual && Math.Abs(data[i + dim] - data[j + dim]) <= tolerance)
                {
                    AppEn_Cum1++;
                    SampEn_Cum1++;
                }
            }

            if (AppEn_Cum > 0 && AppEn_Cum1 > 0)
                sum += Math.Log((double)AppEn_Cum / (double)AppEn_Cum1);
        }

        appEn = sum / (double)(data.Length - dim);
        sampEn = SampEn_Cum > 0 && SampEn_Cum1 > 0 ? Math.Log((double)SampEn_Cum / (double)SampEn_Cum1) : 0.0;

        return (appEn, sampEn);
    }

    /// <summary>
    /// Parallel computes the approximate and sample entropies of a physiological time-series signals (typically used to diagnose diseased states).
    /// ApEn reflects the likelihood that similar patterns of observations will not be followed by additional similar observations. A time series containing many repetitive patterns has a relatively small ApEn; a less predictable process has a higher ApEn.
    /// A smaller value of SampEn also indicates more self-similarity in data set or less noise.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="dim">Embedding dimension</param>
    /// <param name="fTol">Factor to compute the tolerance so that the total is typically equal to 0.2*std</param>
    /// <param name="std">Standard deviation of the population</param>
    /// <returns>AppEn and SampEn</returns>
    public static (double AppEn, double SampEn) Entropy_Parallel(double[] data, CancellationToken ct, uint dim = 2, double fTol = 0.2, double? std = null)
    {
        long upper = data.Length - (dim + 1) + 1;
        double appEn, sampEn;
        double tolerance;
        if (std.HasValue)
            tolerance = std.Value * fTol;
        else
            tolerance = StdDev<double>(data) * fTol;


        int[] AppEn_Cum_Arr = new int[upper];
        int[] AppEn_Cum1_Arr = new int[upper];
        int[] SampEn_Cum_Arr = new int[upper];
        int[] SampEn_Cum1_Arr = new int[upper];
        double[] sum_Arr = new double[upper];
        bool[] isEqual_Arr = new bool[upper];

        Parallel.For(0, upper, i =>
        {
            AppEn_Cum_Arr[i] = 0;
            AppEn_Cum1_Arr[i] = 0;
            for (uint j = 0; j < upper; j++)
            {
                isEqual_Arr[i] = true;
                //m - length series
                for (uint k = 0; k < dim; k++)
                {
                    if (Math.Abs(data[i + k] - data[j + k]) > tolerance)
                    {
                        isEqual_Arr[i] = false;
                        break;
                    }
                    if (ct.IsCancellationRequested)
                        throw new OperationCanceledException("CancelEntropy", ct);
                }
                if (isEqual_Arr[i])
                {
                    AppEn_Cum_Arr[i]++;
                    SampEn_Cum_Arr[i]++;
                }

                //m+1 - length series
                if (isEqual_Arr[i] && Math.Abs(data[i + dim] - data[j + dim]) <= tolerance)
                {
                    AppEn_Cum1_Arr[i]++;
                    SampEn_Cum1_Arr[i]++;
                }
            }
            if (AppEn_Cum_Arr[i] > 0 && AppEn_Cum1_Arr[i] > 0)
                sum_Arr[i] = Math.Log((double)AppEn_Cum_Arr[i] / (double)AppEn_Cum1_Arr[i]);
        });

        appEn = sum_Arr.Sum() / (double)(data.Length - dim);

        ulong SampEn_Cum = 0;
        foreach (int x in SampEn_Cum_Arr)
            SampEn_Cum += (ulong)x;

        ulong SampEn_Cum1 = 0;
        foreach (int x in SampEn_Cum1_Arr)
            SampEn_Cum1 += (ulong)x;

        sampEn = SampEn_Cum > 0 && SampEn_Cum1 > 0 ? Math.Log((double)SampEn_Cum / (double)SampEn_Cum1) : 0.0;

        return (appEn, sampEn);
    }

    /// <summary>
    /// Computes the Shannon entropy, the entropy bit, the ideal entropy, and the ratio Shannon/ideal for a vector of numeric values
    /// </summary>
    /// <param name="data">Numeric values vector</param>
    /// <returns>The Shannon entropy value (bits per symbol), the entropy bit (min. number of bits needed to encode the vector), he ideal entropy, and the ratio Shannon/Ideal</returns>
    public static (double Entropy, double EntropyBit, double IdealEntropy, double EntropyRatio) ShannonEntropy<T>(IEnumerable<T> data)
    {
        double entropy = 0;
        double entropyBit;
        double entropyIdeal;
        double entropyRatio;
        double prob;

        // Convert into an enumerable of doubles.
        IEnumerable<double> values = data.Select(value => Convert.ToDouble(value));
        int nLength = values.Count();

        // Group data values
        // https://stackoverflow.com/questions/20765589/how-do-i-find-duplicates-in-an-array-and-display-how-many-times-they-occurred
        var dict = new Dictionary<double, int>();
        foreach (var value in values)
        {
            // When the key is not found, "count" will be initialized to 0
            dict.TryGetValue(value, out int count);
            dict[value] = count + 1;
        }
        // This also works, but it might be slower since each group in groups need to be counted again
        //var groups = values.GroupBy(v => v);

        // Compute the Shannon entropy
        foreach (var value in dict)
        {
            if (value.Key > 0)
            {
                //prob = value.Value / (value.Key / decimalFactor);
                prob = (double)value.Value / nLength;
                entropy -= prob * Math.Log2(prob);
            }
        }

        // https://github.com/wqyeo/Shannon-Entropy/blob/master/EntropyCal.cs
        // This represents the minimum number of bits to encode the whole vector
        entropyBit = Math.Ceiling(entropy) * nLength;

        // https://stackoverflow.com/questions/2979174/how-do-i-compute-the-approximate-entropy-of-a-bit-string
        // The Shannon entropy if all elements where different
        prob = 1.0 / nLength;
        entropyIdeal = -1.0 * nLength * prob * Math.Log2(prob);

        entropyRatio = entropy / entropyIdeal;

        return (entropy, entropyBit, entropyIdeal, entropyRatio);
    }

    /// <summary>
    /// Computes the standard deviation of a data series.
    /// </summary>
    /// <param name="values">Data values</param>
    /// <param name="asSample"><see langword="True"/> to compute the sample standard deviation (N-1); otherwise (by default), it computes the population (N) deviation</param>
    /// <returns>Standard deviation. A value equal to -1 indicates insufficient data points.</returns>
    public static double StdDev<T>(IEnumerable<T> values, bool asSample = false)
    {
        // Convert into an enumerable of doubles.
        IEnumerable<double> doubles = values.Select(value => Convert.ToDouble(value));

        // Then compute the standard deviation
        double avg = System.Linq.Enumerable.Average(doubles);
        double sum = System.Linq.Enumerable.Sum(System.Linq.Enumerable.Select(doubles, x => (x - avg) * (x - avg)));
        double denominator = values.Count() - (asSample ? 1 : 0);
        return denominator > 0.0 ? Math.Sqrt(sum / denominator) : -1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="ct"></param>
    /// <param name="dim"></param>
    /// <param name="fTol"></param>
    /// <param name="std"></param>
    /// <returns></returns>
    /// <seealso cref="https://www.mdpi.com/1099-4300/24/4/524"/>
    /// <seealso cref="https://github.com/phreer/sampen_estimation"/>
    public static (double AppEn, double SampEn) Entropy_SuperFast(double[] data, CancellationToken ct, EntropyMethod entropyMethod = EntropyMethod.BruteForce, uint dim = 2, double fTol = 0.2, double? std = null)
    {
        int N = data.Length;
        double ApEn, SampEn;
        long A = 0;
        long B = 0;
        double tolerance;
        if (std.HasValue)
            tolerance = std.Value * fTol;
        else
            tolerance = StdDev<double>(data) * fTol;

        // Check we have enough data points
        if (N <= dim) return (-1.0, -1.0);

        // Compute values depending on method
        //long[] AB = ComputeAB_Direct(data.ToList(), dim, tolerance).ToArray();
        long[] AB = entropyMethod switch
        {
            EntropyMethod.BruteForce => ComputeAB_Direct(data.ToList(), dim, tolerance).ToArray(),
            EntropyMethod.MonteCarloUniform => ComputeAB_Uniform(data, dim, tolerance, SampleSize: 1024, SampleNum: 8),
            EntropyMethod.MonteCarloRandom => ComputeAB_QuasiRandom(data, dim, tolerance, SampleSize: 1024, SampleNum: 8, false),
            EntropyMethod.MonteCarloRandomSorted => ComputeAB_QuasiRandom(data, dim, tolerance, SampleSize: 1024, SampleNum: 8, true),
            _ => Array.Empty<long>(),
        };

        int sample_num = AB.Length / 2;

        for (int i = 0; i < sample_num; i++)
        {
            A += AB[i * 2];
            B += AB[i * 2 + 1];
        }
        //if (a) *a = A / sample_num;
        //if (b) *b = B / sample_num;

        if (A > 0 && B > 0)
            SampEn = -Math.Log((double)B / A);
        else
            SampEn = -Math.Log((double)(N - dim - 1) / (N - dim));

        return (0.0, SampEn);
    }

    private static List<long> ComputeAB_Direct(List<double> data, uint m, double r)
    {
        List<PointTree<double, int>> points = GetPoints(data, m + 1);
        return ComputeAB(points, r);
    }

    private static List<PointTree<double, int>> GetPoints(List<double> data, uint m)
    {
        PointTree<double, int>[] result = new PointTree<double, int>[data.Count - (int)m + 1];
        for (int i = 0; i < result.Length; i++)
        {
            //List<double> subData = data.GetRange(i, (int)m);
            //PointTree<double, int> p = new PointTree<double, int>(subData, 0);
            //PointTree<double, int> p = new PointTree<double, int>(data.GetRange(i, (int)m), 0);
            result[i] = new PointTree<double, int>(data.GetRange(i, (int)m), 0);
        }
        return result.ToList();
    }

    private static List<long> ComputeAB(List<PointTree<double, int>> points, double r)
    {
        int n = points.Count;
        List<long> result = new(2) { 0, 0 };
        if (n == 0) return result;
        result = CountMatchedParallel(points, r);
        return result;
    }

    private static List<long> CountMatchedParallel(List<PointTree<double, int>> points, double r)
    {
        int n = points.Count;
        int num_threads = Environment.ProcessorCount;
        if (num_threads == 0) num_threads = 16;
        else if (num_threads > 12) num_threads -= 8;
        else num_threads /= 2;
        if (num_threads > n) num_threads = n / 2;

        List<long> As = new(num_threads);
        List<long> Bs = new(num_threads);
        for (int i = 0; i < num_threads; i++)
        {
            As.Add(0);
            Bs.Add(0);
        }
        
        Parallel.For(0, num_threads, i =>
        {
            CountMatched(points, r, (uint)i, (uint)num_threads, As, Bs);
        });

        //List<Thread> threads = new();
        //for (int i = 0; i < num_threads; i++)
        //{
        //    threads.Add(new Thread(() => CountMatched(points, r, (uint)i, (uint)num_threads, As, Bs)));
        //}
        //foreach (Thread thread in threads)
        //{
        //    thread.Start();
        //}
        //foreach (Thread thread in threads)
        //{
        //    thread.Join();
        //}

        List<long> AB = new() {As.Sum(), Bs.Sum()};
        return AB;
    }

    private static void CountMatched(List<PointTree<double, int>> points, double r, uint offset, uint interval, List<long> As, List<long> Bs)
    {
        uint n = (uint)points.Count;
        uint m = (uint)points[0].Dim() - 1;
        uint index = 0;
        for (uint i = 0; (index = i * interval + offset) < n; ++i)
        {
            PointTree<double, int> p = points[(int)index];
            for (uint j = index + 1; j < n; j++)
            {
                if (p.Within(points[(int)j], (int)m, r))
                {
                    As[(int)offset] += 1;
                    if (-r <= p[(int)m] - points[(int)j][(int)m] && p[(int)m] - points[(int)j][(int)m] <= r)
                    {
                        Bs[(int)offset] += 1;
                    }
                }
            }
        }
    }

    private static long[] ComputeAB_QuasiRandom(double[] data, uint m, double r, int SampleSize = 1024, int SampleNum = 8, bool presort = true)
    {
        List<PointTree<double, int>> points = GetPoints(data.ToList(), m + 1);
        int numPoints = points.Count;
        Random random = new();

        if (presort)
        {
            points.Sort((p1, p2) =>
            {
                for (int i = 0; i < p1.Dim(); i++)
                {
                    if (p1[i] > p2[i])
                        return 1;
                    if (p1[i] < p2[i])
                        return -1;
                }
                return 1;
            });
        }

        long[] ABs = new long[2 * SampleNum];
        //for (int i = 0; i < (2 * SampleNum); ++i)
        //    ABs.Add(0);

        List<PointTree<double, int>> sampled_points = new (SampleSize);
        int[] indices = new int[SampleSize];
        int[] offsets = new int[SampleNum -1];
        int[] tmp_indices = new int[SampleSize];

        for (int j = 0; j < SampleSize; j++)
            indices[j] = random.Next(0, numPoints);
            // indices[j] = (n / sample_size) * j;

        for (int i = 0; i < SampleNum - 1; ++i)
            offsets[i] = random.Next(0, numPoints);

        for (int i = 0; i < SampleNum; i++)
        {
            
            if (i is not 0)
            {
                int offset = offsets[i - 1];
                for (uint j = 0; j < SampleSize; ++j)
                    tmp_indices[j] = (indices[j] + offset) % numPoints;

            }
            else
            {
                for (uint j = 0; j < SampleSize; ++j)
                    tmp_indices[j] = indices[j];
            }

            for (int j = 0; j < SampleSize; ++j)
                sampled_points[j] = points[tmp_indices[j]];

            long[] AB = ComputeAB(sampled_points, r).ToArray();
            ABs[2 * i] += AB[0];
            ABs[2 * i + 1] += AB[1];
        }
        return ABs;
    }

    private static long[] ComputeAB_Uniform(double[] data, uint m, double r, int SampleSize = 1024, int SampleNum = 8)
    {
        List<PointTree<double, int>> points = GetPoints(data.ToList(), m + 1);
        PointTree<double, int>[] sampled_points = new PointTree<double, int>[SampleSize];
        Random random = new();
        int numPoints = points.Count;

        long[] ABs = new long[2 * SampleNum];
        //for (int i = 0; i < (2 * SampleNum); ++i)
        //    ABs.Add(0);
        
        for (int i = 0; i < SampleNum; i++)
        {
            // generate points
            for (int j = 0; j < SampleSize; j++)
                sampled_points[j] = points[random.Next(0, numPoints)];

            long[] AB = ComputeAB(sampled_points.ToList(), r).ToArray();
            ABs[2 * i] += AB[0];
            ABs[2 * i + 1] += AB[1];
        }
    return ABs;
}

}

/// <summary>
/// A point in euclidean space.
/// A class that represents a multi-dimensional euclidean point with some associated value.
/// We allow for each point to have an associated value so that some more information can be stored with each point.
/// Points can also have a multiplicity/count, this corresponds to having several duplicates of the same point.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="S"></typeparam>
public class PointTree<T, S>
    where T : INumber<T>
    where S : INumber<S>
{
    private List<T> vector;
    private S value;
    private int multiplicity;

    /// <summary>
    /// Constructs an empty point. Creates a point in 0 dimensional euclidean space.
    /// This constructor is provided only to make certain edge cases easier to handle.
    /// </summary>
    public PointTree()
    {
        multiplicity = 0;
    }

    /// <summary>
    /// Constructs an empty point. Creates a point with its position in euclidean space defined by vec, value defined by val, and a multiplicity/count of 1.
    /// </summary>
    /// <param name="val">The position in euclidean space</param>
    /// <param name="vec">The value associated with the point</param>
    public PointTree(List<T> vec, S val)
    {
        this.vector = vec;
        this.value = val;
        multiplicity = 1;
    }

    /// <summary>
    /// Constructs an empty point. Copies a point.
    /// </summary>
    /// <param name="val">The position in euclidean space</param>
    /// <param name="vec">The value associated with the point</param>
    public PointTree(PointTree<T, S> p)
    {
        value = p.value;
        vector = p.vector;
        multiplicity = p.Count();
    }

    /// <summary>
    /// Euclidean position of the point.
    /// </summary>
    /// <returns>The euclidean position of the point as a List.</returns>
    public List<T> AsVector()
    {
        return vector;
    }

    /// <summary>
    /// The point's ambient dimension.
    /// </summary>
    /// <returns>the dimension of the space in which the point lives. I.e. a point of the form (1,2,3) lives in dimension 3.</returns>
    public int Dim()
    {
        return vector.Count;
    }
    
    /// <summary>
    /// The point's count/multiplicity.
    /// </summary>
    /// <returns>The count/multiplicity</returns>
    public int Count()
    {
        return multiplicity;
    }

    /// <summary>
    /// Increase the point's count/multiplicity.
    /// </summary>
    /// <param name="n">Amount to increase by</param>
    /// <exception cref="Exception">If <paramref name="n"/> is negative</exception>
    public void IncreaseCountBy(int n)
    {
        if (n < 0)
        {
            throw new Exception("Can't increase by a negative amount");
        }
        multiplicity += n;
    }

    /// <summary>
    /// Increase the point's count/multiplicity by one.
    /// </summary>
    public void IncreaseCountByOne()
    {
        multiplicity += 1;
    }

    /// <summary>
    /// The point's value.
    /// </summary>
    /// <returns>The value stored in the point.</returns>
    public S Value()
    {
        return value;
    }


    public PointTree<T, S> DropLast()
    {
        PointTree<T, S> result = new(this);
        result.vector.RemoveAt(result.vector.Count - 1);
        return result;
    }

    /// <summary>
    /// Index a point. Get the ith coordinate value of the point. I.e. if a point is of the form (4, 5, 6), then its 0th coordinate value is 4 while its 2nd is 6.
    /// </summary>
    /// <param name="index">The coordinate to index</param>
    /// <returns>The coordinate value</returns>
    /// <exception cref="IndexOutOfRangeException">If <paramref name="index"/> is out of bounds</exception>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Dim())
            {
                throw new IndexOutOfRangeException("[] access index for point is out of range.");
            }
            return vector[index];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="p"></param>
    /// <param name="m"></param>
    /// <param name="r"></param>
    /// <returns></returns>
    public bool Within(PointTree<T, S> p, int m, double r)
    {
        
        for (int i = 0; i < m; i++)
        {
            if (p[i] < vector[i] - T.CreateChecked(r) || p[i] > vector[i] + T.CreateChecked(r))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Check for equality. 
    /// Two points are considered equal if they are in the same spot, have the same multiplicity/count, and store the same value.
    /// </summary>
    /// <param name="p">Some other point.</param>
    /// <returns><see cref="true"/>if <paramref name="p"/> equals the current point, otherwise <see cref="false"/>.</returns>
    public bool Equals(PointTree<T, S> p)
    {
        return vector.Equals(p.vector) && multiplicity == p.multiplicity && value.Equals(p.value);
    }

    /// <summary>
    /// Check for inequality.
    /// The opposite of <seealso cref="Equals(PointTree{T, S})"/>.
    /// </summary>
    /// <param name="p">Some other point.</param>
    /// <returns><see cref="false"/> if <paramref name="p"/> equals the current point, otherwise <see cref="true"/>.</returns>
    public bool NotEquals(PointTree<T, S> p)
    {
        return !Equals(p);
    }

    /// <summary>
    /// Prints the point to standard out.
    /// As an example, a point with euclidean location(3,4,5) and with a multiplicity/count of 4 will be printed as (3, 4, 5) : 4
    /// </summary>
    /// <param name="withCount">Whether or not to display the points count/multiplicity.</param>
    public void ToString(bool withCount = true)
    {
        if (vector.Count == 0)
        {
            Console.WriteLine("()");
        }
        else
        {
            Console.Write("(");
            for (int i = 0; i < vector.Count - 1; i++)
            {
                Console.Write(vector[i] + ", ");
            }
            if (withCount)
            {
                Console.WriteLine(vector[^1] + ") : " + multiplicity);
            }
            else
            {
                Console.WriteLine(vector[^1] + ") : ");
            }
        }
    }
}
