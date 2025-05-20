using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public Canvas PouseCanvas;

    public void Update()
    {
        
    }
    public void Pause()
    {
        Time.timeScale = (Time.timeScale == 0f) ? 1f : 0f;
        if (Time.timeScale == 0f)
        {
            PauseNow();
        }
        else
        {
            PouseCanvas.enabled = false;
        }
    }

    private void PauseNow()
    {
        PouseCanvas.enabled = true;

    }
}
