using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL; // LSL���C�u�����̖��O���
using Cysharp.Threading.Tasks;
using TMPro;
using System.Linq;
using System;
using static RuntimeData;

// �X�g���[�����FMATLABtoUnity
public class LSL_MATLAB_UNITY : MonoBehaviour
{
    // �X�g���[����
    [SerializeField] string streamName = "MATLAB_UNITY";
    // �`���l����
    int channelCount = 1;
    // LSL�C�����b�g
    private liblsl.StreamInlet inlet;
    private liblsl.StreamInfo streamInfo;


    // �X�g���[���̌����Ԋu�i�b�j
    [SerializeField] float searchInterval = 2;
    // �f�[�^��M�m�F�̃^�C���A�E�g���ԁi�b�j
    [SerializeField] float dataTimeout = 180;
    // �P��O�̎�M����
    float lastDataTime = 0f;
    // �Đڑ��p
    bool isConnected = false;


    // ����M�f�[�^������o�b�t�@
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


    // ��M�����f�[�^���o�b�t�@�ɗ��߂Ă�
    void Stock()
    {
        //Debug.Log($"aa�P {isConnected}");
        //Debug.Log($"aa�Q {inlet != null}");
        if (!isConnected) return;
        if (inlet == null) return;

        // �^�C���X�^���v���擾�i�^�C���A�E�g��0�b�j
        double timestamp = inlet.pull_sample(oneSmplBuff, 0.0);
        if (timestamp != 0.0) Debug.Log($"aa�R {timestamp != 0.0}");
        if (timestamp == 0.0) return;
        //Debug.Log($"�^�C���X�^���v {timestamp}");

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
        validBuff.Add(smplList);
        Debug.Log($"{logTxt}");
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
        if (isConnected && inlet == null) return;
        if (Time.time - lastDataTime <= dataTimeout) return;
        Debug.LogWarning("�f�[�^�̎�M���^�C���A�E�g���܂����B�X�g���[�����Č������܂��B");
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

            Debug.Log($"����:{Clsfication} �X�R�A:{Scor}");

            switch (Clsfication)
            {
                case 0:
                    ResultTxt.text = "��";
                    if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.white;
                    break;
                case 1:
                    ResultTxt.text = "��";
                    if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.blue;
                    break;
                case 2:
                    ResultTxt.text = "��";
                    if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.white;
                    if (Cube.activeSelf) Cube.transform.position += Vector3.left * 0.015f;
                    break;
                case 3:
                    ResultTxt.text = "��";
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
            Debug.Log("LSL�X�g���[����ؒf���܂����B");
        }
    }
    void OnDestroy()
    {
        Disconnect();
        if (Cube.activeSelf) Cube.GetComponent<Renderer>().material.color = Color.white;
    }

}
