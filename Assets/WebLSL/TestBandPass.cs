using UnityEngine;
using MathNet.Filtering.FIR;
using UniRx;
using System.Collections.Generic;
using System;
using System.Linq;

public class TestBandPass : MonoBehaviour
{
    void Start()
    {
        //double[] a = { 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6 };
        //a = ApplyBandPass(a, 0.2, 25, 1000, 0);
        Exequte();
    }

    public double[] BPF(double[] data, double lowCutoff, double highCutoff, double sampleRate, int order = 0)
    {
        // バンドパスフィルタの設計
        OnlineFirFilter filter = DesignBandPassFilter(lowCutoff, highCutoff, sampleRate, order);

        // フィルタの適用
        double[] filteredData = filter.ProcessSamples(data);

        return filteredData;
    }


    OnlineFirFilter DesignBandPassFilter(double lowCutoff, double highCutoff, double sampleRate, int order = 0)
    {
        // フィルタ係数の設計
        int filterOrder = order; // フィルタの次数
        double[] coefficients = FirCoefficients.BandPass(sampleRate, lowCutoff, highCutoff, filterOrder);

        return new OnlineFirFilter(coefficients);
    }



    //static double[] ApplyBandPassFilter(double[] data, double sampleRate, double lowCutoff, double highCutoff)
    //{
    //    // フィルタの設計
    //    double lowFrequency = 2 * lowCutoff / sampleRate;
    //    double highFrequency = 2 * highCutoff / sampleRate;

    //    AForge.Math.Filters.ButterworthBandPass filter = new AForge.Math.Filters.ButterworthBandPass(lowFrequency, highFrequency);

    //    // フィルタの適用
    //    Complex[] complexData = new Complex[data.Length];
    //    for (int i = 0; i < data.Length; i++)
    //    {
    //        complexData[i] = new Complex(data[i], 0);
    //    }

    //    Complex[] filteredComplexData = filter.Apply(complexData);

    //    // 結果の変換
    //    double[] filteredData = new double[filteredComplexData.Length];
    //    for (int i = 0; i < filteredComplexData.Length; i++)
    //    {
    //        filteredData[i] = filteredComplexData[i].Re;
    //    }

    //    return filteredData;
    //}




    public IntReactiveProperty order = new IntReactiveProperty(0);
    public double x;
    public double y;
    public double z;
    void Exequte()
    {
        double[] a = { 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6 };
        List<double> b = new List<double>();
        order.Subscribe(value =>
        {
            b = BPF(a, 0.2, 25, 1000, value).ToList();
            x = b[0];
            y = b[1];
            z = b[2];
        });
    }

    private void Update()
    {
        if (Math.Abs(4.9112) - Math.Abs(x) < 0.1)
        {
            Debug.Log($"見つけた！{order}");
            return;
        }
        order.Value++;

    }
}
