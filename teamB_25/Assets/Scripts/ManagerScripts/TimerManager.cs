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
    /// �������Ԃ̌v�Z
    /// </summary>
    private void Timer()
    {
        nowTimer -= Time.deltaTime;
        //�������Ԍo��
        if (nowTimer<=0f)
        {
            SceneManager.LoadScene("GameClear");
        }
    }
    /// <summary>
    /// �c�莞�ԕ\�L 
    /// </summary>
    private void TimerText()
    {
        int minute = (int)nowTimer / 60;
        int second = (int)nowTimer % 60;
        timeText.text = $"{minute:00}:{second:00}";
    }
}
