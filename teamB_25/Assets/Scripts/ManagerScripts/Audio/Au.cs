using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Au : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.B))
        {
            AudioManager.Instance.PlayBGM("BGM1");
        }
        if (Input.GetKeyUp(KeyCode.N))
        {
            AudioManager.Instance.PlaySE("ken");
        }
        if (Input.GetKeyUp(KeyCode.M))
        {
            AudioManager.Instance.PlaySE("SE2");
        }
        if (Input.GetKeyUp(KeyCode.L))
        {
            AudioManager.Instance.StopBGM();
        }
    }
}
