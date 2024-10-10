using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL; // LSLライブラリの名前空間
using Cysharp.Threading.Tasks;
using TMPro;
using System.Linq;
using System;
using static RuntimeData;

// ストリーム名：EmotivDataStream-EEG
public class LSL_EMOTIV_UNITY : MonoBehaviour
{
    // ストリーム名
    [SerializeField] string streamName = "EmotivDataStream-EEG";
    // チャネル数
    int channelCount = 1;
    // LSLインレット
    liblsl.StreamInlet inlet;
    liblsl.StreamInfo streamInfo;


    // ストリームの検索間隔（秒）
    [SerializeField] float searchInterval = 1;
    // データ受信確認のタイムアウト時間（秒）
    [SerializeField] float dataTimeout = 3;
    // １回前の受信時刻
    float lastDataTime = 0f;
    // 再接続用
    public static bool isConnected = false;

    // EEGデータを回収するかどうか
    public static bool stockState = false;


    // 毎受信データが入るバッファ
    float[] oneSmplBuff;

    async void Start()
    {
        Connect();
    }

    void Update()
    {
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
        //if (!stockState) return;
        if (!isConnected) return;
        if (inlet == null) return;
            
        double timeStamp = inlet.pull_sample(oneSmplBuff, 0.0);
        if (timeStamp == 0.0) return;
            
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
        smplBuff.Add(smplList);
        //Debug.Log($"{logTxt}");
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
        if (!isConnected || inlet == null) return;
        if (Time.time - lastDataTime <= dataTimeout) return;
        Debug.LogWarning("データの受信がタイムアウトしました。ストリームを再検索します。");
        Disconnect();
        Connect();
        //Stream();
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
    }
}
