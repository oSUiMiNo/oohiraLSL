using Cysharp.Threading.Tasks;
using LSL;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RuntimeData;

public class LSL_UNITY_MATLAB : MonoBehaviour
{
    // �X�g���[����
    [SerializeField] string streamName = "UNITY_MATLAB";
    // �`���l����
    int channelCount = 19;
    // LSL�A�E�g���b�g
    liblsl.StreamOutlet outlet;
    liblsl.StreamInfo streamInfo;
    //liblsl.channel_format_t format = liblsl.channel_format_t.cf_float32; // �f�[�^�^��float32�ɐݒ�
    //liblsl.StreamInfo info = new liblsl.StreamInfo(streamName, streamType, channelCount, nominal_srate, format, "UnityLSL");
    int bagSize = 1;

    public bool Sendable = false;


    private void Start()
    {
        streamInfo = new liblsl.StreamInfo(streamName, "EEG", channelCount);
        // �A�E�g���b�g���쐬
        outlet = new liblsl.StreamOutlet(streamInfo);
    }

    private void Update()
    {
        //SendALL();
        SendLatest();
    }

    public async void SendALL()
    {
        //if (!Sendable) return;
        if(smplBuff.Count == 0) return;
        
        List<float> dat = smplBuff.First();
        smplBuff.RemoveAt(0);

        // �T���v���𑗐M
        outlet.push_sample(dat.ToArray());
    }

    public async void SendLatest()
    {
        //if (!Sendable) return;
        if (smplBuff.Count == 0) return;

        List<float> dat = smplBuff.Last();
        smplBuff.Clear();

        // �T���v���𑗐M
        outlet.push_sample(dat.ToArray());
    }


    void Disconnect()
    {
        if (outlet != null)
        {
            outlet = null;
            Debug.Log("LSL�X�g���[����ؒf���܂����B");
        }
    }


    private void OnDestroy()
    {
        Disconnect();
    }
}
