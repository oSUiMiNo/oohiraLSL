using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UniRx;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using TMPro;


public class UDPReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private int listenPort = 55000; // MATLAB側と一致するポート番号
    //[SerializeField] List<float> validBuffer = new List<float>();
    [SerializeField] List<List<float>> validBuffer = new List<List<float>>();
    [SerializeField] TextMeshProUGUI resultTxt;


    void Start()
    {
        // 受信用のUDPクライアントを初期化
        udpClient = new UdpClient(listenPort);


        // 非同期でデータ受信を開始
        ReceiveDataAsync().Forget();

        Stream();
    }

    int classCount = 3;
    private async UniTaskVoid ReceiveDataAsync()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);

        try
        {
            while (true)
            {
                // 非同期でデータを受信
                UdpReceiveResult result = await udpClient.ReceiveAsync();

                // 受信データをASCII文字列に変換
                string message = Encoding.ASCII.GetString(result.Buffer);

                // Unityのメインスレッドで表示
                Debug.Log("受信データ: " + message);

                JArray classification = JArray.Parse(message);
               
                List<float> oneBuffer = new List<float>();
                for(int i = 0; i < classCount; i++)
                {
                    oneBuffer.Add((float)classification[i]);
                }
                validBuffer.Add(oneBuffer);
                //validBuffer.Add(float.Parse(message));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("UDP Receive Error: " + e.Message);
        }
    }


    async void Stream()
    {
        while (true)
        {
            await UniTask.WaitUntil(() => validBuffer.Count > 0);
            while (validBuffer.Count > 0)
            {
                //Debug.Log($"バッファ {validBuffer[0]}");
                Debug.Log($"分類:{validBuffer[0].IndexOf(validBuffer[0].Max())} スコア:{validBuffer[0].Max()}");
                switch(validBuffer[0].IndexOf(validBuffer[0].Max()))
                {
                    case 0:
                        resultTxt.text = "○";
                        break;
                    case 1:
                        resultTxt.text = "●";
                        break;
                    case 2:
                        resultTxt.text = "←";
                        break;
                    default: 
                        break;
                }

                validBuffer.RemoveAt(0);
                //await UniTask.Delay(TimeSpan.FromSeconds(0.03));
            }
        }
    }


    private void OnApplicationQuit()
    {
        // UDPクライアントを閉じる
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}

