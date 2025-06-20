using System.Collections;
using UnityEngine;
using TMPro;
using UnityEditor.Experimental.GraphView;

public class StartTimer : MonoBehaviour
{
    public static bool IsGameStarted { get; private set; } = false; // ゲームが開始されたかのフラグ

    public Canvas StartCanvas;
    public TextMeshProUGUI startCountText;

    private bool hasStarted = false;

    public float countdownTime = 5f;

    void Start()
    {
        countdownTime = 5f; // 初期化する
        IsGameStarted = false;
        startCountText.gameObject.SetActive(true); // 再表示
    }
    
    void Update()
    {
        // カウントダウン進行中の処理
        if(countdownTime > 0)
        {
            TimerManager.countdownActive = false; //TimerManagerを止める
            countdownTime -= Time.unscaledDeltaTime;
            startCountText.text = Mathf.Ceil(countdownTime).ToString(); 
        }
        // カウントダウン終了後の処理
        else if(!hasStarted)
        {
            hasStarted = true;
            StartPlay();
            startCountText.text = "Start!";
            StartCoroutine(WaitErase());
        }
    }

    // ゲームを開始する処理
    void StartPlay()
    {
        Debug.Log("Start!");

        // ゲーム内の他のコンポーネントを動作させる
        TimerManager.countdownActive = true;
        PlayerBase.countdownActive = true;
        IsGameStarted = true;

        Time.timeScale = 1f; 
    }

    // テキストを１秒表示したのち非表示にする処理
    IEnumerator WaitErase()
    {
        yield return new WaitForSeconds(1f);
        startCountText.gameObject.SetActive(false);
    }
}
