using Cysharp.Threading.Tasks;
using LSL;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeData : MonoBehaviour
{
    public static List<List<float>> smplBuff = new List<List<float>>();
    public static List<List<float>> validBuff = new List<List<float>>();
    public static int Clsfication;
    public static float Scor;

    public static TextMeshProUGUI ResultTxt;
    public static GameObject Cube;
    public static Button Button_Start;
    public static TextMeshProUGUI Message_Sequence;
    public static LSL_UNITY_MATLAB UNITY_MATLAB;

    [SerializeField] TextMeshProUGUI resultTxt;
    [SerializeField] GameObject cube;
    [SerializeField] Button button_Start;
    [SerializeField] TextMeshProUGUI message_Sequence;
    [SerializeField] LSL_UNITY_MATLAB uNITY_MATLAB;

    [SerializeField] int count_SmplBuff;
    [SerializeField] int count_ValidBuff;

    private void Awake()
    {
        ResultTxt = resultTxt;
        Cube = cube;
        Button_Start = button_Start;
        Message_Sequence = message_Sequence;
        UNITY_MATLAB = uNITY_MATLAB;
    }

    void Start()
    {
        //StreamAll();
        //Output_Latest();
    }

    private void Update()
    {
        count_SmplBuff = smplBuff.Count;
        count_ValidBuff = validBuff.Count;
    }



    // バッファにデータが溜まっていたら最新のものだけ流す
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

            Debug.Log($"分類:{Clsfication} スコア:{Scor}");

            switch (Clsfication)
            {
                case 0:
                    resultTxt.text = "○";
                    if (cube.activeSelf) cube.GetComponent<Renderer>().material.color = Color.white;
                    break;
                case 1:
                    resultTxt.text = "●";
                    if (cube.activeSelf) cube.GetComponent<Renderer>().material.color = Color.blue;
                    break;
                case 2:
                    resultTxt.text = "←";
                    if (cube.activeSelf) cube.GetComponent<Renderer>().material.color = Color.white;
                    if (cube.activeSelf) cube.transform.position += new Vector3(-10f, 0, 0);
                    break;
                default:
                    break;
            }

            await Delay.Second(0.07f);
        }
    }


    // バッファにデータが溜まっていたら全部流す
    // エクセルデータの検証などで全結果表示したい場合に使う
    async void Output_All()
    {
        while (true)
        {
            await UniTask.WaitUntil(() => validBuff.Count > 0);
            //while (validBuff.Count > 0)

            List<float> dat = validBuff.First();
            validBuff.RemoveAt(0);

            Scor = dat.Max();
            Clsfication = dat.IndexOf(Scor);

            Debug.Log($"分類:{Clsfication} スコア:{Scor}");

            switch (Clsfication)
            {
                case 0:
                    resultTxt.text = "○";
                    if (cube.activeSelf) cube.GetComponent<Renderer>().material.color = Color.white;
                    break;
                case 1:
                    resultTxt.text = "●";
                    if (cube.activeSelf) cube.GetComponent<Renderer>().material.color = Color.blue;
                    break;
                case 2:
                    resultTxt.text = "←";
                    if (cube.activeSelf) cube.GetComponent<Renderer>().material.color = Color.white;
                    if (cube.activeSelf) cube.transform.position += new Vector3(-10f, 0, 0);
                    break;
                default:
                    break;
            }

            await Delay.Second(0.02f);
        }
    }
}
