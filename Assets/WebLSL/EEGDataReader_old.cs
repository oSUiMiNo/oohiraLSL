using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Mirror;
using MathNet.Filtering.FIR;

public class EEGDataReader_old : MonoBehaviour
{
    string path_DataFolder => $@"{Application.dataPath}\Resources\dat";
    string path_EEGs => $@"{Application.dataPath}\Resources\EEGs";

    [SerializeField] int order = 64;

    void Start()
    {
        AllFileInfo().ForEach(fileInfo =>
        {
            string fileName = fileInfo.Name.Replace(@$"{path_DataFolder}\", string.Empty).Replace(".csv", string.Empty);
            TextAsset file = Resources.Load($"dat/{fileName}") as TextAsset;
            //List<List<double>> csv = ReadCSVFile(file);
            List<double>[] csv = ReadCSVFile(file); 

            Debug.Log(csv.Count());
            Debug.Log(csv[0].Count);
            Debug.Log(csv[0][0]);
            Debug.Log(csv[2][2]);
            Debug.Log(csv[2][2].GetType());

            EEGClass eegClass = new EEGClass(csv, fileInfo, order);
            EEGClass.EEGData.Add(fileName, eegClass);
            //eegClass.Save();
        });
    }



    List<FileInfo> AllFileInfo()
    {
        List<FileInfo> files = new List<FileInfo>();
        foreach (string a in Directory.GetFiles(path_DataFolder, "*.csv"))
        {
            files.Add(new FileInfo(a));
        }
        return files;
    }


    // csvファイルの読み取り
    List<double>[] ReadCSVFile(TextAsset csvFile)
    {
        int range_Col = 19;

        List<List<double>> csvRowsBuffer = new List<List<double>>();
        using (StringReader reader = new StringReader(csvFile.text))
        {
            int count = 0;
            while (reader.Peek() != -1)
            {
                count++;
                string line = reader.ReadLine();
                if (count < 3) { continue; }
                string[] line_splited = line.Split(",");
                //double[] line_parsed = StringArray2DoubleArray(line_splited, 1, range_Col);
                List<double> line_parsed = StringArray2DoubleArray(line_splited, 1, range_Col).ToList();
                csvRowsBuffer.Add(line_parsed);
            }
        }

        List<double>[] csvRows = new List<double>[csvRowsBuffer.Count];
        for(int i =  0; i < csvRowsBuffer.Count; i++)
            csvRows[i] = csvRowsBuffer[i];
        
        return csvRows;
    }

    //// csvファイルの読み取り
    //List<List<double>> ReadCSVFile(TextAsset csvFile)
    //{
    //    int range_Col = 19;
    //    List<List<double>> csvRows = new List<List<double>>();

    //    using (StringReader reader = new StringReader(csvFile.text))
    //    {
    //        int count = 0;
    //        while (reader.Peek() != -1)
    //        {
    //            count++;
    //            string line = reader.ReadLine();
    //            if (count < 3) { continue; }
    //            string[] line_splited = line.Split(",");
    //            //double[] line_parsed = StringArray2DoubleArray(line_splited, 1, range_Col);
    //            List<double> line_parsed = StringArray2DoubleArray(line_splited, 1, range_Col).ToList();
    //            csvRows.Add(line_parsed);
    //        }
    //    }
    //    return csvRows;
    //}



    // string配列をdouble配列に変換
    private double[] StringArray2DoubleArray(string[] arr_in, int colMin, int colMax)
    {
        // 引数のmin,max はCSVの行数なので、配列のインデックス様に帳尻合わせる
        int min = colMin - 1;
        int max = colMax - 1;

        double[] arr_out = new double[max - min + 1];
        for (int i = 0; i < max - min + 1; i++)
        {
            double.TryParse(arr_in[i + min], out arr_out[i]);
        }
        return arr_out;
    }
}


public class EEGClass : Savable
{
    #region Savable の仕込み ===================================
    public override string SaveFolderPath { get; set; } = $"{Application.dataPath}/Resources/EEGs";
    public override List<SaveSystem.IFriendWith_SaveSystem> Instances { get; protected set; } = instances;
    private static List<SaveSystem.IFriendWith_SaveSystem> instances = new();
    #endregion ==============================


    // CSVファイルから読み取ったデータを格納
    public static Dictionary<string, EEGClass> EEGData = new Dictionary<string, EEGClass>();

    public int order;
    FileInfo FileInfo { get; set; }     // ファイル情報
    //List<List<double>> CSV { get; set; }// EEGやタイムスタンプ等が入ったcsvをリストにしたもの
    List<double>[] CSV { get; set; }    // Listの1要素 = 1行（0～19列の１行分の要素の集合;）
    List<List<double>> EEG { get; set; }// 14チャンネルのEEGだけ抜き取ったもの
    float Ts_Ave { get; set; }          // データの平均時間間隔
    //double[,,] Teach { get; set; }      // 学習データ：EEG
    List<double>[,] Teach { get; set; }
    string Valid { get; set; }          // 検証データ：EEG
    List<List<double>> Teach_FFT { get; set; } = new List<List<double>>(14); // 学習データ：FFT
    string Valid_FFT { get; set; }      // 検証データ：FFT
    string NN_Teach { get; set; }       // 学習データ：学習用
    string NN_Valid { get; set; }       // 検証データ：学習用


    public EEGClass(List<double>[] _csv, FileInfo fileInfo, int order)
    {
        this.order = order;
        FileInfo = fileInfo;
        CSV = _csv;
        List<double>[] csv = _csv;

        // データの平均時間間隔を計算
        List<double> diff_times = new List<double>(); // タイムスタンプの差の配列
        //CSV.ForEach(row => diff_times.Add(row[0]));
        for (int i = 0; i < CSV.Count() - 1; i++)
        {
            diff_times.Add(CSV[i + 1][0] - CSV[i][0]);
        }

        //Debug.Log($"diff times {diff_times.Count}");
        //Debug.Log($"diff times {diff_times[0]}");
        //Debug.Log($"diff times {diff_times[1]}");
        //Debug.Log($"diff times {diff_times[2]}");

        Debug.Log(csv.Count());
        Debug.Log(csv[0].Count());

        Ts_Ave = (float)diff_times.Average();
        int sec = 2; // 2秒のデータを切り分け
        int n_Shift = 10; // データを10個ずつシフトして切り出す
        int sec_Teach = 40; // 学習用の時間
        int sec_Valid = 16; // 検証用の時間

        
        int dat_Size = (int)Mathf.Floor(sec / Ts_Ave); // 切り出される1つのデータ数
        // 学習用データ数 40秒分から切り出す
        int count_Teach = (int)Mathf.Floor((sec_Teach / sec * dat_Size - dat_Size) / n_Shift);
        // 検証用データ数 16秒分から切り出す
        int count_Valid = (int)Mathf.Floor((sec_Valid / sec * dat_Size - dat_Size) / n_Shift);
        Debug.Log($"countTeach {count_Teach}");

        Debug.Log($"{dat_Size + n_Shift - n_Shift}");
        Debug.Log($"{dat_Size + n_Shift * 1 - n_Shift * 1}");


        // バンドパス
        for (int i = 0; i < 14; i++)
        {
            List<double> buf = new List<double>();
            for (int j = 0; j < csv.Count(); j++)
            {
                // 基準端子T7,T8のデータを全体から引き、オフセット解消
                csv[j][4 + i] = CSV[j][4 + i] - (CSV[j][9] + CSV[j][14]) / 2;
                buf.Add(csv[j][4 + i]);
            }
            buf = BPF(buf.ToArray(), 0.2, 25, 1 / Ts_Ave).ToList();
            for (int j = 0; j < csv.Count(); j++)
            {
                csv[j][4 + i] = buf[j];
            }
        }

        //Debug.Log(csv[0][4 + 0]);
        //Debug.Log(csv[1][4 + 0]);

        Debug.Log(order);
        double[] a = {1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5, 6, };
        double[] b = BPF(a, 0.2, 25, 1 / Ts_Ave);
        Debug.Log(b[0]);
        Debug.Log(b[1]);
        Debug.Log(b[2]);

        //for (int i = 0; i < 14; i++)
        //{
        //    // バンドパスを適用してノイズ処理
        //    csv[i][4 + i] = MyFilter.BPF(csv[i][4 + j].);

        //}




        // もらったデータから14チャンネルのEEGのみを抜き取る(csvの5-18列)
        //Teach = new double[14, count_Teach, dat_Size + n_Shift - n_Shift];
        Teach = new List<double>[14, count_Teach];
        for (int channel = 0; channel < 14; channel++)
        {
            for (int m = 1; m <= count_Teach; m++)
            {
                Teach[channel, m - 1] = new List<double>();
                for (int i = n_Shift * m; i < dat_Size + n_Shift * m; i++)
                {
                    //if((channel == 0 && m == 1) || (channel == 1 && m == 2))
                    //{
                    //    Debug.Log(channel);
                    //    Debug.Log(m - 1);
                    //    Debug.Log(i);
                    //    Debug.Log(i - n_Shift * m);
                    //    Debug.Log(csv[i][4 + channel]);
                    //}
                    Teach[channel, m - 1].Add(csv[i][4 + channel]);
                }
                FFT(Teach[channel, m - 1], Ts_Ave, 45 * 2);
            }
        }
    }


    void FFT(List<double> x, float ts, int n_FFT)
    {
        float fs = 1 / ts;
        int N = x.Count;

        // 今回使うFFT関数がComplex型を扱うので Double 型から Complex 型の配列に変換
        Complex[] _x = FastFourierTransform.doubleToComplex(x.ToArray());
        // DFT をかける
        Complex[] xDFT = FastFourierTransform.FFT(_x, false);
        // データ数をN / 2 + 1にしてるのはよくわからん
        xDFT = xDFT[0..(N / 2 + 1)];

        List<double> psdx = new List<double>();
        foreach (var a in xDFT)
        {
            psdx.Add((1 / (fs * N)) * Complex.Abs(a) * Complex.Abs(a));
        }
        
        for (int i = 1; i < psdx.Count - 1; i++)
        {
            psdx[i] = 2*psdx[i];
        }

        double[] dat_FFT = new double[psdx.Count];
        for(int i = 0; i < psdx.Count; i++)
        {
            dat_FFT[i] = ToDecibel(psdx[i]) / 2;
        }
        
        dat_FFT = dat_FFT[0..n_FFT];

        //Debug.Log("----------------------------");
        //Debug.Log(dat_FFT[0]);
        //Debug.Log(dat_FFT[1]);
        //Debug.Log(dat_FFT[2]);
    }


    double ToDecibel(double linear)
    {
        double decibel = 0;
        if (linear > 0f)
        {
            decibel = 20f * Math.Log10(linear);
        }
        return decibel;
    }

    double ToDecibel(double linear, double dbMin)
    {
        return Math.Max(ToDecibel(linear), dbMin);
    }

    double FromDecibel(double decibel)
    {
        return Math.Pow(10f, decibel / 20f);
    }




    public double[] BPF(double[] data, double lowCutoff, double highCutoff, double sampleRate)
    {
        // バンドパスフィルタの設計
        OnlineFirFilter filter = DesignBandPassFilter(lowCutoff, highCutoff, sampleRate);

        // フィルタの適用
        double[] filteredData = filter.ProcessSamples(data);

        return filteredData;
    }


    OnlineFirFilter DesignBandPassFilter(double lowCutoff, double highCutoff, double sampleRate)
    {
        // フィルタ係数の設計
        int filterOrder = order; // フィルタの次数
        double[] coefficients = MathNet.Filtering.FIR.FirCoefficients.BandPass(sampleRate, lowCutoff, highCutoff, filterOrder);

        return new OnlineFirFilter(coefficients);
    }
}