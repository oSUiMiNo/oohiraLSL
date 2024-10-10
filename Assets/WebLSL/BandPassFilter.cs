using UnityEngine;
using MathNet.Filtering;
using MathNet.Filtering.FIR;

public class MyFilter
{
    OnlineFirFilter filter;

    //// �t�B���^�p�����[�^
    //double sampleRate = 1000.0; // �T���v�����[�g (Hz)
    //double lowCutoff = 100.0;   // ����g���J�b�g�I�t (Hz)
    //double highCutoff = 300.0;  // �����g���J�b�g�I�t (Hz)

    void Start()
    {
        //// ���̃T���v���f�[�^����
        //double[] data = GenerateSampleData();


        //// �t�B���^�̓K�p
        //double[] filteredData = BPF(data);


        //// �t�B���^���ꂽ�f�[�^�̏o��
        //foreach (var value in filteredData)
        //{
        //    Debug.Log(value);
        //}
    }

    double[] GenerateSampleData()
    {
        // �T���v���f�[�^�𐶐�
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
        // �o���h�p�X�t�B���^�̐݌v
        filter = DesignBandPassFilter(lowCutoff, highCutoff, sampleRate);

        // �t�B���^�̓K�p
        double[] filteredData = filter.ProcessSamples(data);

        return filteredData;
    }

    
    OnlineFirFilter DesignBandPassFilter(double lowCutoff, double highCutoff, double sampleRate)
    {
        // �t�B���^�W���̐݌v
        int filterOrder = 1; // �t�B���^�̎���
        double[] coefficients = MathNet.Filtering.FIR.FirCoefficients.BandPass(sampleRate, lowCutoff, highCutoff, filterOrder);

        return new OnlineFirFilter(coefficients);
    }

}
