using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL; // LSLライブラリの名前空間
using Cysharp.Threading.Tasks;
using TMPro;
using System.Linq;
using System;
using static RuntimeData;

// ストリーム名：MATLABtoUnity
public class LSL_MATLAB_UNITY : MonoBehaviour
{
    // ストリーム名
    [SerializeField] string streamName = "MATLAB_UNITY";
    // チャネル数
    int channelCount = 1;
    // LSLインレット
    private liblsl.StreamInlet inlet;
    private liblsl.StreamInfo streamInfo;


    // ストリームの検索間隔（秒）
    [SerializeField] float searchInterval = 2;
    // データ受信確認のタイムアウト時間（秒）
    [SerializeField] float dataTimeout = 180;
    // １回前の受信時刻
    float lastDataTime = 0f;
    // 再接続用
    bool isConnected = false;


    // 毎受信データが入るバッファ
    float[] oneSmplBuff = new float[19];

    bool stockable = false;
    
    void Start()
    {
        if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.white;

        Button_Start.onClick.AddListener(() =>
        {
            stockable = true;
            Connect();
            Output_Latest();
        });
    }

    void Update()
    {
        if(!stockable) return;
        Stock();
        Reconnect();
    }

    private void OnApplicationQuit()
    {
        Cube.GetComponent<Renderer>().material.color = Color.white;
    }


    // 受信したデータをバッファに溜めてく
    void Stock()
    {
        //Debug.Log($"aa１ {isConnected}");
        //Debug.Log($"aa２ {inlet != null}");
        if (!isConnected) return;
        if (inlet == null) return;

        // タイムスタンプを取得（タイムアウトは0秒）
        double timestamp = inlet.pull_sample(oneSmplBuff, 0.0);
        if (timestamp != 0.0) Debug.Log($"aa３ {timestamp != 0.0}");
        if (timestamp == 0.0) return;
        //Debug.Log($"タイムスタンプ {timestamp}");

        // 受信したデータを処理
        lastDataTime = Time.time;

        string logTxt = $"受信データ：";
        List<float> smplList = new List<float>();
        //Debug.Log(smpl);
        foreach (var elem in oneSmplBuff)
        {
            // 少数第四位を四捨五入
            float elem_Rund = MathF.Round(elem, 4);
            smplList.Add(elem_Rund);
            logTxt += $" [{elem_Rund.ToString("F3")}]";
        }
        validBuff.Add(smplList);
        Debug.Log($"{logTxt}");
    }


    async void Connect()
    {
        while (!isConnected)
        {
            Debug.Log("LSLストリームを検索中...");

            // ストリームを検索
            liblsl.StreamInfo[] streams = liblsl.resolve_stream("name", streamName, channelCount, 5);

            if (streams.Length > 0)
            {
                // 最初のストリームを選択
                streamInfo = streams[0];
                inlet = new liblsl.StreamInlet(streamInfo);

                // バッファの初期化
                oneSmplBuff = new float[streamInfo.channel_count()];

                isConnected = true;
                lastDataTime = Time.time;
                Debug.Log($"LSLストリーム '{streamName}' を見つけました。詳細情報:");
                //Debug.Log($" - Type: {streamInfo.type()}");
                //Debug.Log($" - Channel Count: {streamInfo.channel_count()}");
                //Debug.Log($" - Sampling Rate: {streamInfo.nominal_srate()}");
                //Debug.Log($" - Source ID: {streamInfo.source_id()}");
            }
            else
            {
                Debug.Log($"ストリーム '{streamName}' が見つかりません。{searchInterval}秒後に再試行します。");
                await Delay.Second(searchInterval);
            }
        }
    }

    // ストリームが繋がっていても一定時間データを受け取れなければ再検索
    void Reconnect()
    {
        if (isConnected && inlet == null) return;
        if (Time.time - lastDataTime <= dataTimeout) return;
        Debug.LogWarning("データの受信がタイムアウトしました。ストリームを再検索します。");
        Disconnect();
        Connect();
        //Stream();
    }


    public async void Output_Latest()
    {
        while (true)
        {
            await UniTask.WaitUntil(() => validBuff.Count > 0);
            //while (validBuff.Count > 0)

            List<float> dat = validBuff.Last();
            validBuff.Clear();

            Scor = dat.Max();
            Clsfication = dat.IndexOf(Scor);

            Debug.Log($"分類:{Clsfication} スコア:{Scor}");

            switch (Clsfication)
            {
                case 0:
                    ResultTxt.text = "○";
                    if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.white;
                    break;
                case 1:
                    ResultTxt.text = "●";
                    if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.blue;
                    break;
                case 2:
                    ResultTxt.text = "←";
                    if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.white;
                    if (Cube.activeSelf) Cube.transform.position += Vector3.left * 0.015f;
                    break;
                case 3:
                    ResultTxt.text = "→";
                    if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.white;
                    if (Cube.activeSelf) Cube.transform.position += Vector3.right * 0.015f;
                    break;
                default:
                    break;
            }

            await Delay.Second(0.07f);
        }
    }


    void Disconnect()
    {
        if (inlet != null)
        {
            inlet.close_stream();
            inlet = null;
            isConnected = false;
            Debug.Log("LSLストリームを切断しました。");
        }
    }
    void OnDestroy()
    {
        Disconnect();
        if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.white;
    }

}
