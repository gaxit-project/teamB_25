using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TimerManager : MonoBehaviour
{
    private float startTimer = 180f;
    private float nowTimer;

    public TextMeshProUGUI timeText;

    private void Start()
    {
        SetTimer();
    }
    private void SetTimer()
    {
        nowTimer = startTimer;
    }
    void Update()
    {
        Timer();
        TimerText();
    }
    /// <summary>
    /// 制限時間の計算
    /// </summary>
    private void Timer()
    {
        nowTimer -= Time.deltaTime;
        //制限時間経過
        if (nowTimer<=0f)
        {
            //SceneManager.LoadScene("END");
        }
    }
    /// <summary>
    /// 残り時間表記 
    /// </summary>
    private void TimerText()
    {
        int minute = (int)nowTimer / 60;
        int second = (int)nowTimer % 60;
        timeText.text = $"{minute:00}:{second:00}";
    }
}
