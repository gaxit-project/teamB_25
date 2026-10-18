using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// ゲーム開始前のカウントダウン。終了すると、static フラグでタイマー(TimerManager)と
/// プレイヤー(PlayerBase)に開始を通知する。
/// カウントダウン中は PauseManager.CountDown を true にして、ポーズ画面を開かせない。
/// </summary>
public class StartTimer : MonoBehaviour
{
    public static bool IsGameStarted { get; private set; } = false;

    [Header("UI References")]
    public Canvas StartCanvas;
    public TextMeshProUGUI startCountText;

    [Header("Settings")]
    public float countdownTime = 5f;

    private bool hasStarted = false;

    void Start()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.CountDown = true;
        }

        // static フラグは前回プレイの値が残るため、開始時に初期化する
        TimerManager.countdownActive = false;
        IsGameStarted = false;
        hasStarted = false;
        startCountText.gameObject.SetActive(true);
    }

    void Update()
    {
        if (countdownTime > 0)
        {
            // ポーズ入力などで timeScale が 0 になっていても進める
            countdownTime -= Time.unscaledDeltaTime;
            startCountText.text = Mathf.Ceil(countdownTime).ToString();
        }
        else if (!hasStarted)
        {
            hasStarted = true;
            StartPlay();
            startCountText.text = "Start!";
            StartCoroutine(WaitErase());
        }
    }

    // カウントダウン終了。他のコンポーネントへフラグで通知し、時間を通常に戻す
    void StartPlay()
    {
        TimerManager.countdownActive = true;
        PlayerBase.countdownActive = true;

        IsGameStarted = true;
        Time.timeScale = 1f;
    }

    // "Start!" を1秒表示してから消し、ポーズを許可する
    IEnumerator WaitErase()
    {
        yield return new WaitForSeconds(1f);
        startCountText.gameObject.SetActive(false);
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.CountDown = false;
        }
    }
}