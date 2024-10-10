using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL; // LSL���C�u�����̖��O���
using Cysharp.Threading.Tasks;
using TMPro;
using System.Linq;
using System;
using static RuntimeData;

// �X�g���[�����FEmotivDataStream-EEG
public class LSL_EMOTIV_UNITY : MonoBehaviour
{
    // �X�g���[����
    [SerializeField] string streamName = "EmotivDataStream-EEG";
    // �`���l����
    int channelCount = 1;
    // LSL�C�����b�g
    liblsl.StreamInlet inlet;
    liblsl.StreamInfo streamInfo;


    // �X�g���[���̌����Ԋu�i�b�j
    [SerializeField] float searchInterval = 1;
    // �f�[�^��M�m�F�̃^�C���A�E�g���ԁi�b�j
    [SerializeField] float dataTimeout = 3;
    // �P��O�̎�M����
    float lastDataTime = 0f;
    // �Đڑ��p
    public static bool isConnected = false;

    // EEG�f�[�^��������邩�ǂ���
    public static bool stockState = false;


    // ����M�f�[�^������o�b�t�@
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

    
    // ��M�����f�[�^���o�b�t�@�ɗ��߂Ă�
    void Stock()
    {
        //if (!stockState) return;
        if (!isConnected) return;
        if (inlet == null) return;
            
        double timeStamp = inlet.pull_sample(oneSmplBuff, 0.0);
        if (timeStamp == 0.0) return;
            
        // ��M�����f�[�^������
        lastDataTime = Time.time;

        string logTxt = $"��M�f�[�^�F";
        List<float> smplList = new List<float>();
        //Debug.Log(smpl);
        foreach (var elem in oneSmplBuff)
        {
            // ������l�ʂ��l�̌ܓ�
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
            Debug.Log("LSL�X�g���[����������...");

            // �X�g���[��������
            liblsl.StreamInfo[] streams = liblsl.resolve_stream("name", streamName, channelCount, 5);

            if (streams.Length > 0)
            {
                // �ŏ��̃X�g���[����I��
                streamInfo = streams[0];
                inlet = new liblsl.StreamInlet(streamInfo);

                // �o�b�t�@�̏�����
                oneSmplBuff = new float[streamInfo.channel_count()];

                isConnected = true;
                lastDataTime = Time.time;
                Debug.Log($"LSL�X�g���[�� '{streamName}' �������܂����B�ڍ׏��:");
                //Debug.Log($" - Type: {streamInfo.type()}");
                //Debug.Log($" - Channel Count: {streamInfo.channel_count()}");
                //Debug.Log($" - Sampling Rate: {streamInfo.nominal_srate()}");
                //Debug.Log($" - Source ID: {streamInfo.source_id()}");
            }
            else
            {
                Debug.Log($"�X�g���[�� '{streamName}' ��������܂���B{searchInterval}�b��ɍĎ��s���܂��B");
                await Delay.Second(searchInterval);
            }
        }
    }

    // �X�g���[�����q�����Ă��Ă���莞�ԃf�[�^���󂯎��Ȃ���΍Č���
    void Reconnect()
    {
        if (!isConnected || inlet == null) return;
        if (Time.time - lastDataTime <= dataTimeout) return;
        Debug.LogWarning("�f�[�^�̎�M���^�C���A�E�g���܂����B�X�g���[�����Č������܂��B");
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
            Debug.Log("LSL�X�g���[����ؒf���܂����B");
        }
    }

    void OnDestroy()
    {
        Disconnect();
    }
}
