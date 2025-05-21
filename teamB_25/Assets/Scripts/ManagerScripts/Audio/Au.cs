using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Au : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            AudioManager.Instance.PlayBGM("BGM1");
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            AudioManager.Instance.PlaySE("SE1");
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            AudioManager.Instance.PlaySE("SE2");
        }
    }
}
