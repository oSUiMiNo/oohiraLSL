using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using LSL;
using Assets.LSL4Unity.Scripts.AbstractInlets;
//using System.Numerics;
//using System.Numerics;



public class PlotEEG : ADoubleInlet
{
    // ■記録用変数
    private List<double[]> csv_list = new List<double[]>();     // CSVファイルから読み取ったデータを格納
    private List<double[]> eeg_list = new List<double[]>();     // 取得したEEGのデータを格納(仮想状態、実測ともに使用)
    [SerializeField] private int csv_count = 0;
    [SerializeField] private bool export = true;
    [SerializeField] private string export_file_name = "test";
    [SerializeField] private RecordState now_record_state = RecordState.Wating;
    [SerializeField] private Environment now_record_environment = Environment.CSV;
    //[SerializeField] private GameObject line_obj;    // 大平コメントアウト

    private Dictionary<string, GameObject> line_dictionary = new Dictionary<string, GameObject>();

    // 大平
    [SerializeField] private Color lineColor = Color.black;
    [SerializeField] private float lineThickness = 0.05f;
    [SerializeField] private Vector2 lineSize = new Vector2(100, 12);
    [SerializeField] private Vector2 linePosition = new Vector2(-30, 20);
    [SerializeField] private float betweenLines = 7;
    [SerializeField] private int len = 200;      // 表示されているLineの中にいくつ文のデータの波形を表示するか
    MyExtention monoBehaviourEX = new MyExtention();


    double[] X_inputValues;
    double[] Y_inputValues;
    double[] Y_output;



    private void Start()
    {
        ReadCSVFile();
        InputEventHandler.OnDown_Space += () => GetInput();
    }

    private void Update()
    {
        //GetInput();
    }

    private void FixedUpdate()
    {
        GetEEG();
        //int len = 200;

        for (int i = 0; i < 14; i++)
        {
            Vector2 linePosition = new Vector2(this.linePosition.x, this.linePosition.y * 2 - betweenLines * i);
            SetPlot($"test{i}", CreateConsecutiveArray(len), ExtractArrayFromList(eeg_list, 4+i, len), linePosition, lineSize, lineColor, lineThickness);
        }
    }
    // ◆プロセス
    protected override void Process(double[] newSample, double timeStamp)
    {
    }

    // ■入力
    private void GetInput()
    {
        switch (now_record_state)
        {
            case RecordState.Wating:
                now_record_state = RecordState.Recording;
                eeg_list = new List<double[]>();
                break;
            case RecordState.Recording:
                now_record_state = RecordState.Stopping;
                break;
        }
    }

    // ■データの入力
    private void GetEEG()
    {
        switch (now_record_environment)
        {
            case Environment.CSV:
                GetEEGFromCSV();
                break;
            case Environment.EpocXEEG:
                GetEEGFromEpocX();
                break;
        }
    }

    private void GetEEGFromCSV()
    {
        switch (now_record_state)
        {
            case RecordState.Recording:
                SetCSVData();
                //Debug.Log(string.Join(',', eeg_list[eeg_list.Count - 1]));
                break;
            case RecordState.Stopping:
                now_record_state = RecordState.Wating;
                if (export == false) { break; }
                ExportCsv(eeg_list, $"{GetNowTime()}_{export_file_name}");
                break;
        }
    }

    private void GetEEGFromEpocX()
    {
        switch (now_record_state)
        {
            case RecordState.Recording:
                break;
            case RecordState.Stopping:
                now_record_state = RecordState.Wating;
                if (export == false) { break; }
                ExportCsv(eeg_list, $"{GetNowTime()}_{export_file_name}");
                break;
        }
    }

    // ◆データの出力
    private void ExportCsv(List<double[]> data, string file_name)
    {
        StringBuilder string_builder = new StringBuilder();
        foreach (double[] arr in data)
        {
            string line = string.Join(",", arr);
            string_builder.AppendLine(line);
        }
        File.WriteAllText($"{Application.dataPath}/Resources/dat/{file_name}.csv", string_builder.ToString());
    }

    // 現在時刻
    private string GetNowTime()
    {
        TimeZoneInfo time_zone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
        DateTime utc_now = DateTime.UtcNow;
        DateTime data_time = TimeZoneInfo.ConvertTimeFromUtc(utc_now, time_zone);
        return data_time.ToString("yyMMdd_HHmmss");
    }

    // ◆CSVファイルから擬似的に脳波のデータを作成
    // csvファイルの読み取り
    private void ReadCSVFile()
    {
        TextAsset csv_file;
        string file_name = "231110_01_free";
        csv_file = Resources.Load(file_name) as TextAsset;
        StringReader reader = new StringReader(csv_file.text);
        int count = 0;
        while (reader.Peek() != -1)
        {
            count++;
            string line = reader.ReadLine();
            if (count < 3) { continue; }
            string[] line_splited = line.Split(",");
            double[] line_parsed = StringArray2DoubleArray(line_splited, new Vector2Int(0, 19));
            csv_list.Add(line_parsed);
        }
    }

    // string配列をdouble配列に変換
    private double[] StringArray2DoubleArray(string[] arr_in, Vector2Int range)
    {
        double[] arr_out = new double[range.y - range.x + 1];
        for (int i = 0; i < range.y - range.x + 1; i++)
        {
            double.TryParse(arr_in[i + range.x], out arr_out[i]);
        }
        return arr_out;
    }

    // csvの情報をEEGの配列に追加
    private void SetCSVData()
    {
        if (csv_list.Count == 0) { return; }
        eeg_list.Add(csv_list[csv_count]);
        csv_count = (csv_count + 1) % csv_list.Count;
    }

    // ◆グラフ表示
    private void SetPlot(string line_obj_key, double[] x, double[] y,Vector2 anchor, Vector2 size, Color color, float lineThickness=0.1f)
    {
        //// フーリエ変換
        //System.Numerics.Complex[] inputSignal_Time = FastFourierTransform.doubleToComplex(y);
        //System.Numerics.Complex[] outputSignal_Freq = FastFourierTransform.FFT(inputSignal_Time, false);
        
        //Y_output = new double[len];
        ////get module of complex number
        //for (int i = 0; i < len; i++)
        //{
        //    Y_output[i] = (double)System.Numerics.Complex.Abs(outputSignal_Freq[i]);
        //}

        //string y_out = string.Empty;
        //foreach(var a in Y_output)
        //{
        //    y_out += $"{a.ToString()}\n";
        //}
        //Debug.Log($"{y_out}");




        GameObject obj;
        if (line_dictionary.ContainsKey(line_obj_key) == true) { obj = line_dictionary[line_obj_key]; }
        else
        {
            //obj = Instantiate(line_obj);　// 大平コメントアウト
            obj = new GameObject($"LineObj_{line_obj_key}");　// 大平
            line_dictionary[line_obj_key] = obj;
        }
        int plot_count = (int)MathF.Min(x.Length,y.Length);
        //LineRenderer line_renderer = obj.GetComponent<LineRenderer>();　// 大平コメントアウト
        LineRenderer line_renderer = monoBehaviourEX.CheckAddComponent<LineRenderer>(obj);　// 大平
        Renderer renderer = obj.GetComponent<Renderer>();
        renderer.material.color = color;
        line_renderer.positionCount = plot_count;
        line_renderer.startWidth = lineThickness;
        line_renderer.endWidth = lineThickness;

        float y_min = (float)y[0];
        float x_min = (float)x[0];
        float y_max = 0;
        float x_max = 0;
        for (int i = 0; i < plot_count; i++)
        {
            y_min = Mathf.Min((float)y_min, (float)y[i]);
            y_max = Mathf.Max((float)y_max, (float)y[i]);
            x_min = Mathf.Min((float)x_min, (float)x[i]);
            x_max = Mathf.Max((float)x_max, (float)x[i]);
        }
        float height = y_max - y_min;
        float width = x_max - x_min;
        if (height < 1) { height = 1; }
        if (width < 1) { width = 1; }
        for (int i = 0; i < plot_count; i++)
        {
            line_renderer.SetPosition(i, new Vector2(
                anchor.x + (float)(x[i] - x_min) / width * size.x,
                anchor.y + (float)(y[i] - y_min) / height * size.y
                ));
        }
    }
    //
    private double[] ExtractArrayFromList(List<double[]> list, int col, int max_length)
    {
        double[] array = new double[max_length];
        int count = max_length;
        for (int i = list.Count-1; i >= Mathf.Max(0,list.Count-max_length); i--)
        {
            count--;
            array[count] = list[i][col];
        }
        return array;
    }
    private double[] CreateConsecutiveArray(int length)
    {
        double[] array = new double[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = i;
        }
        return array;
    }

    // ◆enum

    private enum RecordState
    {
        Wating=0,
        Recording=1,
        Stopping=2
    }
    private enum Environment 
    {
        CSV=0,
        EpocXEEG=1
    }
}

/*

[RequireComponent(typeof(LineRenderer))]
public class LineGraph : MonoBehaviour
{
    public int numPoints = 100;
    public float xRange = 10f;
    public float yRange = 5f;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = numPoints;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        PlotGraph();
    }

    void PlotGraph()
    {
        Vector3[] points = new Vector3[numPoints];

        for (int i = 0; i < numPoints; i++)
        {
            float x = i * (xRange / (numPoints - 1));
            float y = Mathf.Sin(x); // ここで任意の関数に変更可能
            points[i] = new Vector3(x, y, 0);
        }

        lineRenderer.SetPositions(points);
    }
}

 */