using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using UnityEngine;

public class MATLABClient : MonoBehaviour
{
    void Start()
    {
        // UDP�N���C�A���g���쐬
        UdpClient udpClient = new UdpClient(55000); // �|�[�g�ԍ���MATLAB�ƈ�v������
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        Debug.Log("�f�[�^�҂� ...");

        while (true)
        {
            // �f�[�^����M
            byte[] receiveBytes = udpClient.Receive(ref remoteEP);
            string receiveData = Encoding.ASCII.GetString(receiveBytes);

            // ��M�����f�[�^��\��
            Debug.Log($"��M����: {receiveData}");
        }
    }
}
