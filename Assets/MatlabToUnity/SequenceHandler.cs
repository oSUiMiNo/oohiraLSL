using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using static RuntimeData;
using UnityEngine.UI;
using System;
using UniRx;

public class SequenceHandler : MonoBehaviour
{
    //Button button_Start => GameObject.Find("Button_Start").GetComponent<Button>();
    bool CubeIsMovable = false;

    [SerializeField] float moveTime = 5f;
    [SerializeField] float stockDelay = 0.5f;
    void Start()
    {
        Button_Start.onClick.AddListener(() => Exe());
    }

    async void Exe()
    {
        Button_Start.gameObject.SetActive(false);
        await Delay.Second(0.3f);
        Message_Sequence.text = "3";
        await Delay.Second(1);
        Message_Sequence.text = "2";
        await Delay.Second(1);
        Message_Sequence.text = "1";
        await Delay.Second(1);
        Message_Sequence.text = "";
        await OneSeq(Vector3.left);

        Button_Start.gameObject.SetActive(true);
    }

    async UniTask OneSeq(Vector3 moveDirection)
    {
        Cube.SetActive(true);
       
        Observable.EveryFixedUpdate()
                .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(moveTime)))
                .Subscribe(_ => MoveCube(moveDirection)).AddTo(gameObject);
                       
        Observable.Timer(TimeSpan.FromSeconds(stockDelay)).Subscribe(_ =>
            Observable.EveryFixedUpdate()
                .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(moveTime - stockDelay * 2)))
                .Subscribe(_ => MoveCube(moveDirection)).AddTo(gameObject));

        Observable.EveryFixedUpdate()
         .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(moveTime)))
         .Subscribe(_ => UNITY_MATLAB.SendALL()).AddTo(gameObject);

        await Delay.Second(moveTime);
        Cube.SetActive(false);
    }

    void MoveCube(Vector3 direction)
    {
        Cube.transform.position += direction * 0.01f;
    }
}
