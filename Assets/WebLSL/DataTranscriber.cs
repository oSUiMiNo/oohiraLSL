using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Mirror;
using NobleConnect;
using NobleConnect.Mirror;
using Cysharp.Threading.Tasks;

public class DataTranscriber : MonoBehaviour
{
    [SerializeField] WebLSL webLSL;

    /// <summary>
    /// number of channels
    /// </summary>
    public Text NumChans;

    /// <summary>
    /// unique id of sender
    /// </summary>
    public Text DeviceID;

    /// <summary>
    /// header of data
    /// </summary>
    public Text DataHeaderTxt;

    /// <summary>
    /// data stream
    /// </summary>
    public Text DataStreamTxt;

    void Start()
    {
        IPPublisher.On_NetworkRoleSet.Subscribe(async _ =>
        {
            await UniTask.WaitUntil(() => GameObject.Find("PlayerServer") != null);
            webLSL = GameObject.Find("PlayerServer").GetComponent<WebLSL>();

            webLSL.NumChans.Subscribe(value => NumChans.text = value);
            webLSL.DeviceID.Subscribe(value => DeviceID.text = value);
            webLSL.DataHeaderTxt.Subscribe(value => DataHeaderTxt.text = value);
            webLSL.DataStreamTxt.Subscribe(value => DataStreamTxt.text = value);
        });

    }
}
