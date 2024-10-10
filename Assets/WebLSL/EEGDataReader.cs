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
            else Debug.LogError($"default, free, gaze, left, right �̂����ꂩ���t�@�C�����Ɋ܂߂āB");

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
    public FileInfo _FileInfo { get; set; }      // �t�@�C�����
    public string FileName { get; set; }       // �t�@�C����
    public string _Image { get; set; }           // ���ޖ�
    public bool Use {  get; set; }               // �g�����ǂ���
    public float[] ReadRange_Teach { get; set; } // �w�K�p�͈�
    public float[] ReadRange_Valid { get; set; } // ���ؗp�͈�
}



public class EEGModel : Savable
{
    #region Savable �̎d���� ===================================
    public override string SaveFolderPath { get; set; } = $"{Application.dataPath}/Resources/EEGs";
    public override List<SaveSystem.IFriendWith_SaveSystem> Instances { get; protected set; } = instances;
    private static List<SaveSystem.IFriendWith_SaveSystem> instances = new();
    #endregion ==============================


    // CSV�t�@�C������ǂݎ�����f�[�^���i�[
    public static Dictionary<string, EEGModel> EEGData = new Dictionary<string, EEGModel>();

    // CSV���X�g�z��́A�z���1�v�f = 1�s�i0�`19��̂P�s���̗v�f����ׂ����X�g�j
    // �g�p���́ACSV[�z���index][���X�g��index]
    // �`���l�����͕s�ς����A�]�g�̓��͓͂��I�ł��邱�Ƃ��l����ƁA
    // �z��ƃ��X�g�̎�]�֌W���t�ɂ����`���ɕς���ׂ�����
    public List<double>[] CSV { get; private set; } // EEG��^�C���X�^���v����������csv�����X�g�ɂ�������
    public EEGOption OptionData { get; private set; }           // �I�v�V����
    public float Ts_Ave { get; private set; }                   // �T���v�����O����
    public FileInfo _FileInfo { get; private set; }
    public string _Image { get; private set; }                  // �z�N�����C���[�W
    //public List<List<double>> Split_Data { get; set; }  // �؂蕪����ꂽEEG�f�[�^
    //public List<List<double>> Split_FFT { get; set; }   // Split_Data�ɑ΂���FFT���������f�[�^
    public List<double>[,] Split_Data { get; private set; }
    public List<double>[,] Split_FFT { get; private set; }
    public List<List<double>> Data_Teach { get; private set; }  // �w�K�p�f�[�^
    public List<List<double>> Data_Valid { get; private set; }  // ���ؗp�f�[�^
    public int DataCount { get; private set; }                 // �؂蕪����ꂽ�f�[�^��


    // �؂�o�����P�f�[�^������̕b��
    int sec = 2;
    // �g�p����ő�̎��g��
    int f_END = 45;


    public EEGModel(EEGOption optionData, int n_Shift, int sec, int f_END)
    {
        this.sec = sec;
        this.f_END = f_END;

        // �t�@�C���̓ǂݎ�� ________________________________
        OptionData = optionData;
        _FileInfo  = optionData._FileInfo;
        _Image = optionData._Image;
        TextAsset file = Resources.Load($"dat/{optionData.FileName}") as TextAsset;
        CSV = ReadCSVFile(file);
        
        // ���Ԃ̓ǂݎ�� ____________________________________
        // �f�[�^�̕��ώ��ԊԊu���v�Z
        List<double> diff_times = new List<double>(); // �^�C���X�^���v�̍��̔z��
        //CSV.ForEach(row => diff_times.Add(row[0]));
        for (int i = 0; i < CSV.Count() - 1; i++)
        {
            diff_times.Add(CSV[i + 1][0] - CSV[i][0]);
        }
        Ts_Ave = (float)diff_times.Average();

        // �f�[�^�̐؂蕪�� __________________________________
        // �؂�o�����1�̃f�[�^��
        int blockSize = (int)Mathf.Floor(sec / Ts_Ave);
        // �؂蕪���鐔
        DataCount = Mathf.Min
        (
            60000, 
            (int)Mathf.Floor((CSV.Count() - 3 - blockSize) / n_Shift)
        );

        // ��������f�[�^����14�`�����l����EEG�݂̂𔲂����(csv��5-18��)
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

    // csv�t�@�C���̓ǂݎ��
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
        // �`���l��(���index)���Ƃ�sec* f_END�̃f�[�^�������Ă���B������`���l�����Ƃł͂Ȃ��P��ɂ���B
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


    // string�z���double�z��ɕϊ�
    private double[] StringArray2DoubleArray(string[] arr_in, int colMin, int colMax)
    {
        // ������min,max ��CSV�̍s���Ȃ̂ŁA�z��̃C���f�b�N�X�l�ɒ��K���킹��
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

        // ����g��FFT�֐���Complex�^�������̂� Double �^���� Complex �^�̔z��ɕϊ�
        Complex[] _x = FastFourierTransform.doubleToComplex(x.ToArray());
        // DFT ��������
        Complex[] xDFT = FastFourierTransform.FFT(_x, false);
        // �f�[�^����N / 2 + 1�ɂ��Ă�̂͂悭�킩���
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
    //    // �o���h�p�X�t�B���^�̐݌v
    //    OnlineFirFilter filter = DesignBandPassFilter(lowCutoff, highCutoff, sampleRate);

    //    // �t�B���^�̓K�p
    //    double[] filteredData = filter.ProcessSamples(data);

    //    return filteredData;
    //}


    //OnlineFirFilter DesignBandPassFilter(double lowCutoff, double highCutoff, double sampleRate)
    //{
    //    // �t�B���^�W���̐݌v
    //    int filterOrder = order; // �t�B���^�̎���
    //    double[] coefficients = MathNet.Filtering.FIR.FirCoefficients.BandPass(sampleRate, lowCutoff, highCutoff, filterOrder);

    //    return new OnlineFirFilter(coefficients);
    //}
}