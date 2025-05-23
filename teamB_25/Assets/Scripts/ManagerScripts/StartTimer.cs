using System.Collections;
using UnityEngine;
using TMPro;

public class StartTimer : MonoBehaviour
{
    public static bool IsGameStarted { get; private set; } = false;

    public Canvas StartCanvas;
    public TextMeshProUGUI startCountText;

    public float countdownTime = 5f;

    void Start()
    {
        
    }

    
    void Update()
    {
        if(countdownTime > 0)
        {
            TimerManager.countdownActive = false; //TimerManagerを止める
            countdownTime -= Time.deltaTime;
            startCountText.text = Mathf.Ceil(countdownTime).ToString(); 
        }
        else if(countdownTime <= 0)
        {
            StartPlay();
            startCountText.text = "Start!";
            StartCoroutine(WaitErase());
        }
    }

    void StartPlay()
    {
        Debug.Log("Start!");
        TimerManager.countdownActive = true;
        PlayerBase.countdownActive = true;
        IsGameStarted = true;
    }

    IEnumerator WaitErase()
    {
        yield return new WaitForSeconds(1f);
        startCountText.gameObject.SetActive(false);
    }
}
