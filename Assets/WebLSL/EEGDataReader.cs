using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Mirror;
using MathNet.Filtering.FIR;
using UnityEngine.UIElements;
using static UnityEditorInternal.ReorderableList;
using Cysharp.Threading.Tasks;

public class EEGDataReader : MonoBehaviour
{
    string path_DataFolder => $@"{Application.dataPath}\Resources\dat";
    string path_EEGs => $@"{Application.dataPath}\Resources\EEGs";

    List<FileInfo> AllFileInfo 
    {
        get
        {
            List<FileInfo> files = new List<FileInfo>();
            foreach (string a in Directory.GetFiles(path_DataFolder, "*.csv"))
            {
                files.Add(new FileInfo(a));
            }
            return files;
        }
    }

    void Start()
    {
        AllFileInfo.ForEach(fileInfo =>
        {
            string imame = string.Empty;
            if (fileInfo.Name.Contains("default")) imame = "default";
            else
            if (fileInfo.Name.Contains("free")) imame = "free";
            else
            if (fileInfo.Name.Contains("gaze")) imame = "gaze";
            else
            if (fileInfo.Name.Contains("left")) imame = "left";
            else
            if (fileInfo.Name.Contains("right")) imame = "right";
            else Debug.LogError($"default, free, gaze, left, right のいずれかをファイル名に含めて。");

            EEGOption eegOption = new EEGOption()
            {
                _FileInfo = fileInfo,
                FileName = fileInfo.Name.Replace(".csv", string.Empty),
                _Image = imame,
                Use = true,
                ReadRange_Teach = new float[] { 0, 0.7f },
                ReadRange_Valid = new float[] { 0.7f, 1 }
            };
            EEGModel eegModel = new EEGModel(eegOption, 10, 2, 45);
            EEGModel.EEGData.Add(eegOption._FileInfo.Name, eegModel);
        });

    }
}



public class EEGOption
{
    public FileInfo _FileInfo { get; set; }      // ファイル情報
    public string FileName { get; set; }       // ファイル名
    public string _Image { get; set; }           // 分類名
    public bool Use {  get; set; }               // 使うかどうか
    public float[] ReadRange_Teach { get; set; } // 学習用範囲
    public float[] ReadRange_Valid { get; set; } // 検証用範囲
}



public class EEGModel : Savable
{
    #region Savable の仕込み ===================================
    public override string SaveFolderPath { get; set; } = $"{Application.dataPath}/Resources/EEGs";
    public override List<SaveSystem.IFriendWith_SaveSystem> Instances { get; protected set; } = instances;
    private static List<SaveSystem.IFriendWith_SaveSystem> instances = new();
    #endregion ==============================


    // CSVファイルから読み取ったデータを格納
    public static Dictionary<string, EEGModel> EEGData = new Dictionary<string, EEGModel>();

    // CSVリスト配列は、配列の1要素 = 1行（0～19列の１行分の要素を並べたリスト）
    // 使用時は、CSV[配列のindex][リストのindex]
    // チャネル数は不変だが、脳波の入力は動的であることを考えると、
    // 配列とリストの主従関係を逆にした形式に変えるべきかも
    public List<double>[] CSV { get; private set; } // EEGやタイムスタンプ等が入ったcsvをリストにしたもの
    public EEGOption OptionData { get; private set; }           // オプション
    public float Ts_Ave { get; private set; }                   // サンプリング周期
    public FileInfo _FileInfo { get; private set; }
    public string _Image { get; private set; }                  // 想起したイメージ
    //public List<List<double>> Split_Data { get; set; }  // 切り分けられたEEGデータ
    //public List<List<double>> Split_FFT { get; set; }   // Split_Dataに対してFFTをかけたデータ
    public List<double>[,] Split_Data { get; private set; }
    public List<double>[,] Split_FFT { get; private set; }
    public List<List<double>> Data_Teach { get; private set; }  // 学習用データ
    public List<List<double>> Data_Valid { get; private set; }  // 検証用データ
    public int DataCount { get; private set; }                 // 切り分けられたデータ数


    // 切り出した１データあたりの秒数
    int sec = 2;
    // 使用する最大の周波数
    int f_END = 45;


    public EEGModel(EEGOption optionData, int n_Shift, int sec, int f_END)
    {
        this.sec = sec;
        this.f_END = f_END;

        // ファイルの読み取り ________________________________
        OptionData = optionData;
        _FileInfo  = optionData._FileInfo;
        _Image = optionData._Image;
        TextAsset file = Resources.Load($"dat/{optionData.FileName}") as TextAsset;
        CSV = ReadCSVFile(file);
        
        // 時間の読み取り ____________________________________
        // データの平均時間間隔を計算
        List<double> diff_times = new List<double>(); // タイムスタンプの差の配列
        //CSV.ForEach(row => diff_times.Add(row[0]));
        for (int i = 0; i < CSV.Count() - 1; i++)
        {
            diff_times.Add(CSV[i + 1][0] - CSV[i][0]);
        }
        Ts_Ave = (float)diff_times.Average();

        // データの切り分け __________________________________
        // 切り出される1つのデータ数
        int blockSize = (int)Mathf.Floor(sec / Ts_Ave);
        // 切り分ける数
        DataCount = Mathf.Min
        (
            60000, 
            (int)Mathf.Floor((CSV.Count() - 3 - blockSize) / n_Shift)
        );

        // もらったデータから14チャンネルのEEGのみを抜き取る(csvの5-18列)
        List<double>[,] split_Data = new List<double>[14, DataCount];
        List<double>[,] split_FFT = new List<double>[14, DataCount];
        for (int channel = 0; channel < 14; channel++)
        {
            for (int count = 1; count <= DataCount; count++)
            {
                int read_start = 1 + (count - 1) * n_Shift;
                split_Data[channel, count - 1] = new List<double>();
                split_FFT[channel, count - 1] = new List<double>();
                for (int i = read_start; i < read_start + blockSize; i++)
                {
                    split_Data[channel, count - 1].Add(CSV[i - 1][4 + channel]);
                }
                split_FFT[channel, count - 1] = FFT(split_Data[channel, count - 1], Ts_Ave, sec * f_END);
                //for(int i = 0; i < blockSize; i++)
                //{
                //    Split_Data[channel, i] = split_Data[channel, count - 1];

                //}
            }
        }
        Split_Data = split_Data; // Debug.Log($"{Split_Data[13, 763][255]}");
        Split_FFT = split_FFT;

        Debug.Log($"_____________________________");
        Debug.Log($"{Split_FFT.GetLength(0)}");
        Debug.Log($"{Split_FFT.GetLength(1)}");
        Debug.Log($"{Split_FFT[0, 0].Count}");
        Debug.Log($"{Split_FFT[0, 0][0]}");
        Debug.Log($"{Split_FFT[0, 0][1]}");
        Debug.Log($"{Split_FFT[13, 763][89]}");

        Debug.Log($"{Split_FFT[0, 0][0]}");
        Debug.Log($"{Split_FFT[0, 0][1]}");
        Debug.Log($"{Split_FFT[0, 1][0]}");
        Debug.Log($"{Split_FFT[0, 1][1]}");


        SplitData_T_V(optionData, sec);
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
        for (int i = 0; i < csvRowsBuffer.Count; i++)
            csvRows[i] = csvRowsBuffer[i];

        return csvRows;
    }

    void SplitData_T_V(EEGOption optionData, int sec)
    {
        int len_Dat = (int)Mathf.Floor(sec * f_END);
        float lenCount_Teach = (int)Mathf.Floor(DataCount * (optionData.ReadRange_Teach[1] - optionData.ReadRange_Teach[0]));
        float lenCount_Valid = (int)Mathf.Floor(DataCount * (optionData.ReadRange_Valid[1] - optionData.ReadRange_Valid[0]));
        int start_Teach = (int)Mathf.Floor(DataCount * optionData.ReadRange_Teach[0]) + 1;
        int start_Valid = (int)Mathf.Floor(DataCount * optionData.ReadRange_Valid[0]) + 1;
        Data_Teach = new List<List<double>>();
        Data_Valid = new List<List<double>>();
        Debug.Log($"_____________________________");
        Debug.Log($"{start_Teach + lenCount_Teach - 1}");
        //Debug.Log($"{Split_FFT.GetLength(1)}");
        Debug.Log($"{len_Dat}");
        // チャネル(第一index)ごとにsec* f_ENDのデータが入っている。それをチャネルごとではなく１列にする。
            List<double> split_FFT_Squeezed = new List<double>();
        for (int channel = 0; channel < 14; channel++)
        {
            //Debug.Log($"{channel + 1}");
            //for(int i = ((channel) * len_Dat) + 1; i < channel + 1 * len_Dat; i++)
            //{
            // Squeeze
                
            for (int j = start_Teach; j < start_Teach + lenCount_Teach - 1; j++)
            {
                for (int k = 0; k < Split_FFT[0, 0].Count; k++)
                {
                    split_FFT_Squeezed.Add(Split_FFT[channel, j - 1][k]);
                    if (channel < 2 && j - 1 < 2 && k < 2)
                    {
                        Debug.Log($"{channel}_____________________________");
                        Debug.Log($"{channel}");
                        Debug.Log($"{j - 1}");
                        Debug.Log($"{k}");
                        Debug.Log($"{Split_FFT[channel, j - 1][k]}");
                        Debug.Log($"{split_FFT_Squeezed[k]}");
                    }
                }
            }

           
        }
        for (int channel = 0; channel < 14; channel++)
        {
            Debug.Log($"{((channel + 1) * len_Dat + 1) - (((channel) * len_Dat) + 1)}");
            Debug.Log($"{(((channel) * len_Dat) + 1)}");
            Debug.Log($"{((channel + 1) * len_Dat + 1)}");


            for (int i = ((channel) * len_Dat) + 1; i < (channel + 1) * len_Dat + 1; i++)
            {
                Data_Teach.Add(new List<double>());
                //Data_Teach[i].AddRange(split_FFT_Squeezed);
                //if (channel == 0) Debug.Log($"{i}");
                //Debug.Log($"{split_FFT_Squeezed.Count}");
                //Debug.Log($"{i - 1}");
                //Data_Teach[i - 1] = split_FFT_Squeezed;
                Data_Teach[i - 1].AddRange(split_FFT_Squeezed);


                //for(int j = 0; j < 3; j++)
                //{
                //    Debug.Log($"{split_FFT_Squeezed[j]}");
                //}
                //Debug.Log($"{Data_Teach[0][0]}");
                //for (int j = 1; j < lenCount_Teach; j++)
                //{
                //}

                //for (int j = start_Valid; j < start_Valid + lenCount_Valid - 1; j++)
                //{
                //    for (int k = 0; k < Split_FFT.GetLength(1); k++)
                //    {
                //        split_FFT_Squeezed.Add(Split_FFT[channel, k][j - 1]);
                //    }
                //}
                //for (int j = 1; j < lenCount_Valid; j++)
                //{
                //    Data_Valid[i].AddRange(split_FFT_Squeezed);
                //}
            }
        }
        //Debug.Log($"{Data_Teach.Count}");
        //Debug.Log($"{Data_Teach[0].Count}");
        ////Debug.Log($"{Data_Teach[0][0]}");
        ////Debug.Log($"{Data_Teach[1][0]}");
        ////Debug.Log($"{Data_Teach[0][1]}");
        ////Debug.Log($"{Data_Teach[1][1]}");

        //Debug.Log($"{Data_Teach[0][0]}");
        //Debug.Log($"{Data_Teach[0][1]}");
        //Debug.Log($"{Data_Teach[1][0]}");
        //Debug.Log($"{Data_Teach[1][1]}");
        //Debug.Log($"{Data_Teach[88][47969]}");

        string a = "";
        for(int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                a += ", " + Data_Teach[i][j].ToString();
            }
            Debug.Log($"__________________________");
            Debug.Log($"{a}");
            a = "";
        }
    }


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




    List<double> FFT(List<double> x, float ts, int n_FFT)
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

        return dat_FFT.ToList();
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




    //public double[] BPF(double[] data, double lowCutoff, double highCutoff, double sampleRate)
    //{
    //    // バンドパスフィルタの設計
    //    OnlineFirFilter filter = DesignBandPassFilter(lowCutoff, highCutoff, sampleRate);

    //    // フィルタの適用
    //    double[] filteredData = filter.ProcessSamples(data);

    //    return filteredData;
    //}


    //OnlineFirFilter DesignBandPassFilter(double lowCutoff, double highCutoff, double sampleRate)
    //{
    //    // フィルタ係数の設計
    //    int filterOrder = order; // フィルタの次数
    //    double[] coefficients = MathNet.Filtering.FIR.FirCoefficients.BandPass(sampleRate, lowCutoff, highCutoff, filterOrder);

    //    return new OnlineFirFilter(coefficients);
    //}
}