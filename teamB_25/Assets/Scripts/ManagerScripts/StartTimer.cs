using System.Collections;
using TMPro;
using UnityEngine;

public class StartTimer : MonoBehaviour
{
    public static bool IsGameStarted { get; private set; } = false;

    public Canvas StartCanvas;
    public TextMeshProUGUI startCountText;

    private bool hasStarted = false;
    public float countdownTime = 5f;

    void Start()
    {
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
        PlayerBase.countdownActive = true;

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
