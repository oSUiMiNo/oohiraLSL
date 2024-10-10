using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RuntimeData;

public class Move : MonoBehaviour
{

    void Update()
    {
        Cube.transform.position += Vector3.right * 0.0002f;
    }
}
