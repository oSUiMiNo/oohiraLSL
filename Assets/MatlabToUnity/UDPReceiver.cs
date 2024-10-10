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
    private int listenPort = 55000; // MATLAB���ƈ�v����|�[�g�ԍ�
    //[SerializeField] List<float> validBuffer = new List<float>();
    [SerializeField] List<List<float>> validBuffer = new List<List<float>>();
    [SerializeField] TextMeshProUGUI resultTxt;


    void Start()
    {
        // ��M�p��UDP�N���C�A���g��������
        udpClient = new UdpClient(listenPort);


        // �񓯊��Ńf�[�^��M���J�n
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
                // �񓯊��Ńf�[�^����M
                UdpReceiveResult result = await udpClient.ReceiveAsync();

                // ��M�f�[�^��ASCII������ɕϊ�
                string message = Encoding.ASCII.GetString(result.Buffer);

                // Unity�̃��C���X���b�h�ŕ\��
                Debug.Log("��M�f�[�^: " + message);

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
                //Debug.Log($"�o�b�t�@ {validBuffer[0]}");
                Debug.Log($"����:{validBuffer[0].IndexOf(validBuffer[0].Max())} �X�R�A:{validBuffer[0].Max()}");
                switch(validBuffer[0].IndexOf(validBuffer[0].Max()))
                {
                    case 0:
                        resultTxt.text = "��";
                        break;
                    case 1:
                        resultTxt.text = "��";
                        break;
                    case 2:
                        resultTxt.text = "��";
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
        // UDP�N���C�A���g�����
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}

