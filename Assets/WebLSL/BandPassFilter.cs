using UnityEngine;
using MathNet.Filtering;
using MathNet.Filtering.FIR;

public class MyFilter
{
    OnlineFirFilter filter;

    //// フィルタパラメータ
    //double sampleRate = 1000.0; // サンプルレート (Hz)
    //double lowCutoff = 100.0;   // 低周波数カットオフ (Hz)
    //double highCutoff = 300.0;  // 高周波数カットオフ (Hz)

    void Start()
    {
        //// 仮のサンプルデータ生成
        //double[] data = GenerateSampleData();


        //// フィルタの適用
        //double[] filteredData = BPF(data);


        //// フィルタされたデータの出力
        //foreach (var value in filteredData)
        //{
        //    Debug.Log(value);
        //}
    }

    double[] GenerateSampleData()
    {
        // サンプルデータを生成
        int length = 1024;
        double[] data = new double[length];
        System.Random rand = new System.Random();

        for (int i = 0; i < length; i++)
        {
            data[i] = Mathf.Sin(2 * Mathf.PI * 150 * i / 1000) + 0.5 * rand.NextDouble();
        }

        return data;
    }

    public double[] BPF(double[] data, double lowCutoff, double highCutoff, double sampleRate)
    {
        // バンドパスフィルタの設計
        filter = DesignBandPassFilter(lowCutoff, highCutoff, sampleRate);

        // フィルタの適用
        double[] filteredData = filter.ProcessSamples(data);

        return filteredData;
    }

    
    OnlineFirFilter DesignBandPassFilter(double lowCutoff, double highCutoff, double sampleRate)
    {
        // フィルタ係数の設計
        int filterOrder = 1; // フィルタの次数
        double[] coefficients = MathNet.Filtering.FIR.FirCoefficients.BandPass(sampleRate, lowCutoff, highCutoff, filterOrder);

        return new OnlineFirFilter(coefficients);
    }

}
