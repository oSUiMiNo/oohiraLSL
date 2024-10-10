using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;
using NobleConnect.Mirror;
using NobleConnect;
using UniRx;
using Google.Protobuf.WellKnownTypes;
using Ookii.Dialogs;

public class WebLSL : NetworkBehaviour
{
    //[SerializeField]
    public StringReactiveProperty NumChans;
    [SerializeField]
    public StringReactiveProperty DeviceID;
    [SerializeField]    
    public StringReactiveProperty DataHeaderTxt;
    [SerializeField]
    public StringReactiveProperty DataStreamTxt;
    [SerializeField]
    public StringReactiveProperty DropdownStreamsText;

    [SyncVar(hook = "HookReactiveSyncVar_NumChans")]
    string NumChans_Sync;
    [SyncVar(hook = "HookReactiveSyncVar_DeviceID")]
    string DeviceID_Sync;
    [SyncVar(hook = "HookReactiveSyncVar_DataHeaderTxt")]
    string DataHeaderTxt_Sync;
    [SyncVar(hook = "HookReactiveSyncVar_DataStreamTxt")]
    string DataStreamTxt_Sync;
    [SyncVar(hook = "HookReactiveSyncVar_DropdownStreamsText")]
    string DropdownStreamsText_Sync;


    DataReceiver Receiver => GameObject.Find("DataReceiver").GetComponent<DataReceiver>();

    NobleNetworkManager networkManager;
    IPPublisher ipPublisher;
  

    void Start()
    {
        networkManager = (NobleNetworkManager)NetworkManager.singleton;
        ipPublisher = GameObject.Find("IPPublisher").GetComponent<IPPublisher>();
        Debug.Log($"{0}");
        Debug.Log($"{0}{ipPublisher.networkRole}");

        if (isLocalPlayer)
        {
            Receiver.NumChans.Subscribe(value => NumChans_Sync = value);
            Receiver.DeviceID.Subscribe(value => DeviceID_Sync = value);
            Receiver.DataHeaderTxt.Subscribe(value => DataHeaderTxt_Sync = value);
            Receiver.DataStreamTxt.Subscribe(value => DataStreamTxt_Sync = value);
            Receiver.DropdownStreams.onValueChanged.AddListener((int a) => DropdownStreamsText_Sync = Receiver.DropdownStreams.captionText.text);
        }

        Debug.Log($"{1}{ipPublisher.networkRole}");
        if (ipPublisher.networkRole == NetworkRole.Host)
        {
            Debug.Log($"{2}");
            if (isLocalPlayer) playerName = "PlayerServer";
            else playerName = "PlayerClient";
            gameObject.name = playerName;
            CmdOnNameChanged(playerName);
        }
        if (ipPublisher.networkRole == NetworkRole.Client)
        {
            Debug.Log($"{3}");
            if (isLocalPlayer) playerName = "PlayerClient";
            else playerName = "PlayerServer";
            gameObject.name = playerName;
            CmdOnNameChanged(playerName);
        }
    }

    [SyncVar(hook = nameof(HookOnNameChanged))]
    public string playerName;

    public override void OnStartLocalPlayer()
    {
        //Debug.Log($"{1}{ipPublisher.networkRole}");
        //if(ipPublisher.networkRole == NetworkRole.Host)
        //{
        //    Debug.Log($"{2}");
        //    if (isLocalPlayer) playerName = "PlayerServer";
        //    else playerName = "PlayerClient";
        //    gameObject.name = playerName;
        //    CmdOnNameChanged(playerName);
        //}
        //if (ipPublisher.networkRole == NetworkRole.Client)
        //{
        //    Debug.Log($"{3}");
        //    if (isLocalPlayer) playerName = "PlayerClient";
        //    else playerName = "PlayerServer";
        //    gameObject.name = playerName;
        //    CmdOnNameChanged(playerName);
        //}
    }

    void HookOnNameChanged(string oldValue, string newValue)
    {
        playerName = newValue;
        gameObject.name = playerName;
    }

    [Command]
    void CmdOnNameChanged(string name)
    {
        playerName = name;
    }

    //public override void OnStartServer()
    //{
    //    if (isLocalPlayer)
    //    {
    //        CmdChangeClientPlayerName("ServerPlayer");
    //    }
    //}

    //public override void OnStartClient()
    //{
    //    if (isLocalPlayer)
    //    {
    //        RpcChangeServert; PlayerName("ClientPlayer");
    //    }
    //}





    void HookReactiveSyncVar_NumChans(string oldValue, string newValue)
    {
        //NumChans_Sync = newValue; // Ç±ÇÍÇèëÇ©Ç»Ç¢Ç∆NumChans_Syncé©ëÃÇÕìØä˙Ç≥ÇÍÇ»Ç¢ÇÁÇµÇ¢
        NumChans.Value = newValue;
    }
    void HookReactiveSyncVar_DeviceID(string oldValue, string newValue)
    {
        DeviceID.Value = newValue;
    }
    void HookReactiveSyncVar_DataHeaderTxt(string oldValue, string newValue)
    {
        DataHeaderTxt.Value = newValue;
    }
    void HookReactiveSyncVar_DataStreamTxt(string oldValue, string newValue)
    {
        DataStreamTxt.Value = newValue;
    }
    void HookReactiveSyncVar_DropdownStreamsText(string oldValue, string newValue)
    {
        DropdownStreamsText.Value = newValue;
    }
}
