using System.Collections;
using TMPro;
using UnityEngine;

public class StartTimer : MonoBehaviour
{
    public static bool IsGameStarted { get; private set; } = false;

    [Header("UI References")]
    public Canvas StartCanvas;
    public TextMeshProUGUI startCountText;

    [Header("Settings")]
    public float countdownTime = 5f;

    private bool hasStarted = false;
    private PlayerBase player; // プレイヤー参照

    void Start()
    {
        // プレイヤー取得（シーンに一人だけいる前提）
        player = FindObjectOfType<PlayerBase>();
        if (player == null)
        {
            Debug.LogError("PlayerBase がシーンに見つかりません。StartTimer が動作しません。");
        }

        PauseManager.Instance.CountDown = true;
        countdownTime = 5f;
        IsGameStarted = false;
        hasStarted = false;
        startCountText.gameObject.SetActive(true);
    }

    void Update()
    {
        if (countdownTime > 0)
        {
            TimerManager.countdownActive = false;
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

    void StartPlay()
    {
        Debug.Log("Start!");

        // 他のコンポーネントへフラグ通知
        TimerManager.countdownActive = true;
        if (player != null)
        {
            PlayerBase.countdownActive = true; ; // ← PlayerBase のインスタンスを参照
        }

        IsGameStarted = true;
        Time.timeScale = 1f;
    }

    IEnumerator WaitErase()
    {
        yield return new WaitForSeconds(1f);
        startCountText.gameObject.SetActive(false);
        PauseManager.Instance.CountDown = false;
    }
}
