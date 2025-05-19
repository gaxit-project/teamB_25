using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public Canvas PouseCanvas;
    public void Pouse()
    {
        Time.timeScale = (Time.timeScale == 0f) ? 1f : 0f;
        if (Time.timeScale == 0f)
        {
            PouseNow();
        }
        else
        {
            PouseCanvas.enabled = false;
        }
    }

    private void PouseNow()
    {
        PouseCanvas.enabled = true;
    }
}
