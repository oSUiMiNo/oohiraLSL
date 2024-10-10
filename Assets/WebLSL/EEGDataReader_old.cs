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
        for(int i =  0; i < csvRowsBuffer.Count; i++)
            csvRows[i] = csvRowsBuffer[i];
        
        return csvRows;
    }

    //// csv�t�@�C���̓ǂݎ��
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
}


public class EEGClass : Savable
{
    #region Savable �̎d���� ===================================
    public override string SaveFolderPath { get; set; } = $"{Application.dataPath}/Resources/EEGs";
    public override List<SaveSystem.IFriendWith_SaveSystem> Instances { get; protected set; } = instances;
    private static List<SaveSystem.IFriendWith_SaveSystem> instances = new();
    #endregion ==============================


    // CSV�t�@�C������ǂݎ�����f�[�^���i�[
    public static Dictionary<string, EEGClass> EEGData = new Dictionary<string, EEGClass>();

    public int order;
    FileInfo FileInfo { get; set; }     // �t�@�C�����
    //List<List<double>> CSV { get; set; }// EEG��^�C���X�^���v����������csv�����X�g�ɂ�������
    List<double>[] CSV { get; set; }    // List��1�v�f = 1�s�i0�`19��̂P�s���̗v�f�̏W��;�j
    List<List<double>> EEG { get; set; }// 14�`�����l����EEG�����������������
    float Ts_Ave { get; set; }          // �f�[�^�̕��ώ��ԊԊu
    //double[,,] Teach { get; set; }      // �w�K�f�[�^�FEEG
    List<double>[,] Teach { get; set; }
    string Valid { get; set; }          // ���؃f�[�^�FEEG
    List<List<double>> Teach_FFT { get; set; } = new List<List<double>>(14); // �w�K�f�[�^�FFFT
    string Valid_FFT { get; set; }      // ���؃f�[�^�FFFT
    string NN_Teach { get; set; }       // �w�K�f�[�^�F�w�K�p
    string NN_Valid { get; set; }       // ���؃f�[�^�F�w�K�p


    public EEGClass(List<double>[] _csv, FileInfo fileInfo, int order)
    {
        this.order = order;
        FileInfo = fileInfo;
        CSV = _csv;
        List<double>[] csv = _csv;

        // �f�[�^�̕��ώ��ԊԊu���v�Z
        List<double> diff_times = new List<double>(); // �^�C���X�^���v�̍��̔z��
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
        int sec = 2; // 2�b�̃f�[�^��؂蕪��
        int n_Shift = 10; // �f�[�^��10���V�t�g���Đ؂�o��
        int sec_Teach = 40; // �w�K�p�̎���
        int sec_Valid = 16; // ���ؗp�̎���

        
        int dat_Size = (int)Mathf.Floor(sec / Ts_Ave); // �؂�o�����1�̃f�[�^��
        // �w�K�p�f�[�^�� 40�b������؂�o��
        int count_Teach = (int)Mathf.Floor((sec_Teach / sec * dat_Size - dat_Size) / n_Shift);
        // ���ؗp�f�[�^�� 16�b������؂�o��
        int count_Valid = (int)Mathf.Floor((sec_Valid / sec * dat_Size - dat_Size) / n_Shift);
        Debug.Log($"countTeach {count_Teach}");

        Debug.Log($"{dat_Size + n_Shift - n_Shift}");
        Debug.Log($"{dat_Size + n_Shift * 1 - n_Shift * 1}");


        // �o���h�p�X
        for (int i = 0; i < 14; i++)
        {
            List<double> buf = new List<double>();
            for (int j = 0; j < csv.Count(); j++)
            {
                // ��[�qT7,T8�̃f�[�^��S�̂�������A�I�t�Z�b�g����
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
        //    // �o���h�p�X��K�p���ăm�C�Y����
        //    csv[i][4 + i] = MyFilter.BPF(csv[i][4 + j].);

        //}




        // ��������f�[�^����14�`�����l����EEG�݂̂𔲂����(csv��5-18��)
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
        // �o���h�p�X�t�B���^�̐݌v
        OnlineFirFilter filter = DesignBandPassFilter(lowCutoff, highCutoff, sampleRate);

        // �t�B���^�̓K�p
        double[] filteredData = filter.ProcessSamples(data);

        return filteredData;
    }


    OnlineFirFilter DesignBandPassFilter(double lowCutoff, double highCutoff, double sampleRate)
    {
        // �t�B���^�W���̐݌v
        int filterOrder = order; // �t�B���^�̎���
        double[] coefficients = MathNet.Filtering.FIR.FirCoefficients.BandPass(sampleRate, lowCutoff, highCutoff, filterOrder);

        return new OnlineFirFilter(coefficients);
    }
}