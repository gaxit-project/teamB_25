using System.Collections;
using UnityEngine;
using TMPro;

public class StartTimer : MonoBehaviour
{
    public static bool IsGameStarted { get; private set; } = false; // ゲームが開始されたかのフラグ

    public Canvas StartCanvas;
    public TextMeshProUGUI startCountText;

    public float countdownTime = 5f;

    void Start()
    {
        
    }

    
    void Update()
    {
        // カウントダウン進行中の処理
        if(countdownTime > 0)
        {
            TimerManager.countdownActive = false; //TimerManagerを止める
            countdownTime -= Time.deltaTime;
            startCountText.text = Mathf.Ceil(countdownTime).ToString(); 
        }
        // カウントダウン終了後の処理
        else if(countdownTime <= 0)
        {
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
    }

    // テキストを１秒表示したのち非表示にする処理
    IEnumerator WaitErase()
    {
        yield return new WaitForSeconds(1f);
        startCountText.gameObject.SetActive(false);
    }
}
