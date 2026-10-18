using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 制限時間タイマー。StartTimer のカウントダウンが終わると動き始め、0 になると
/// Sleep シーン(ゲームオーバー演出)へ遷移する。
/// ポーズ中は Time.deltaTime が 0 になるため、タイマーも止まる。
/// </summary>
public class TimerManager : MonoBehaviour
{
    public static bool countdownActive = false; // StartTimer のカウントダウン終了で true になる

    [SerializeField] private float startTimer;

    private float nowTimer;

    public TextMeshProUGUI timeText;

    private void Start()
    {
        SetTimer();
        timeText.gameObject.SetActive(false);
    }

    private void SetTimer()
    {
        nowTimer = startTimer;
    }

    void Update()
    {
        if (!countdownActive) return;

        timeText.gameObject.SetActive(true);
        Timer();
        TimerText();
    }

    // 残り時間を減らし、0 になったらゲームオーバー演出へ遷移する
    private void Timer()
    {
        nowTimer -= Time.deltaTime;

        if (nowTimer <= 0f)
        {
            SceneManager.LoadScene("Sleep");
        }
    }

    // 残り時間を m:ss 形式で表示する
    private void TimerText()
    {
        int minute = (int)nowTimer / 60;
        int second = (int)nowTimer % 60;
        timeText.text = $"{minute:0}:{second:00}";
    }
}