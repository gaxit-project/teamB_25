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
            AudioManager.Instance.PlaySE("ken",transform.position);
        }
        if (Input.GetKeyUp(KeyCode.M))
        {
            AudioManager.Instance.PlaySE("SE2", transform.position);
        }
        if (Input.GetKeyUp(KeyCode.L))
        {
            AudioManager.Instance.StopBGM();
        }
        if(Input.GetKeyUp(KeyCode.R))
        {
            AudioManager.Instance.PlaySELoop("SELoop", transform);
        }
        if (Input.GetKeyUp(KeyCode.G))
        {
            AudioManager.Instance.DestroySE(name);
        }
    }
}
