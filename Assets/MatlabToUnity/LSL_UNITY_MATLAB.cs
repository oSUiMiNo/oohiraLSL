using Cysharp.Threading.Tasks;
using LSL;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RuntimeData;

public class LSL_UNITY_MATLAB : MonoBehaviour
{
    // ストリーム名
    [SerializeField] string streamName = "UNITY_MATLAB";
    // チャネル数
    int channelCount = 19;
    // LSLアウトレット
    liblsl.StreamOutlet outlet;
    liblsl.StreamInfo streamInfo;
    //liblsl.channel_format_t format = liblsl.channel_format_t.cf_float32; // データ型をfloat32に設定
    //liblsl.StreamInfo info = new liblsl.StreamInfo(streamName, streamType, channelCount, nominal_srate, format, "UnityLSL");
    int bagSize = 1;

    public bool Sendable = false;


    private void Start()
    {
        streamInfo = new liblsl.StreamInfo(streamName, "EEG", channelCount);
        // アウトレットを作成
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

        // サンプルを送信
        outlet.push_sample(dat.ToArray());
    }

    public async void SendLatest()
    {
        //if (!Sendable) return;
        if (smplBuff.Count == 0) return;

        List<float> dat = smplBuff.Last();
        smplBuff.Clear();

        // サンプルを送信
        outlet.push_sample(dat.ToArray());
    }


    void Disconnect()
    {
        if (outlet != null)
        {
            outlet = null;
            Debug.Log("LSLストリームを切断しました。");
        }
    }


    private void OnDestroy()
    {
        Disconnect();
    }
}
