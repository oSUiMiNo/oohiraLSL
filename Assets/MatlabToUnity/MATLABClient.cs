using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using UnityEngine;

public class MATLABClient : MonoBehaviour
{
    void Start()
    {
        // UDPクライアントを作成
        UdpClient udpClient = new UdpClient(55000); // ポート番号はMATLABと一致させる
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        Debug.Log("データ待ち ...");

        while (true)
        {
            // データを受信
            byte[] receiveBytes = udpClient.Receive(ref remoteEP);
            string receiveData = Encoding.ASCII.GetString(receiveBytes);

            // 受信したデータを表示
            Debug.Log($"受信した: {receiveData}");
        }
    }
}
