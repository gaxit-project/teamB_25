using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// ブレーカー復旧の進行管理。全ブレーカーが復旧したら、脱出扉をランダムに1つ開放し、
/// 扉の演出カメラを短時間表示する。
/// 連携: Breaker(復旧を通知), DoorOpener(扉の開閉), MapManager(ゴールマーカー)
/// </summary>
public class BreakerManager : MonoBehaviour
{
    public static bool countdownActive = false; // StartTimerを待つ

    public static BreakerManager Instance;
    public static int _breakerTask = 4;
    public static int _breakerOn = 0;
    private bool exitOpened = false; // 脱出扉を開く処理が二重に走らないようにする
    public TextMeshProUGUI _breakertext;
    public GameObject[] exitObjects; // 扉が開くアニメーションに使用するオブジェクト
    public GameObject[] flag; // 扉に触れたか判定するオブジェクト

    public Camera[] exitCameras; // 脱出扉ごとのカメラ
    public Camera playerCamera; // プレイヤーのカメラ
    public float cinematicDuration = 2f;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _breakerOn = 0;
        exitOpened = false;
        if (_breakertext != null)
        {
            _breakertext.text = $"{_breakerOn} / {_breakerTask}";
        }

        // 全ブレーカー復旧まで、脱出扉と判定オブジェクトは非表示にしておく
        foreach (GameObject obj in exitObjects)
        {
            obj.SetActive(false);
        }

        foreach (GameObject f in flag)
        {
            f.SetActive(false);
        }

        // プレイヤーカメラのみ有効にする
        playerCamera.enabled = true;
        foreach (Camera cam in exitCameras)
            cam.enabled = false;
    }

    public void ActivateBreaker()
    {
        _breakerOn++;

        if (_breakertext != null)
        {
            _breakertext.text = $"{_breakerOn} / {_breakerTask}";
        }

        // 全ブレーカーが復旧したら、脱出扉をランダムに1つ開く
        if (!exitOpened && _breakerOn == _breakerTask)
        {
            exitOpened = true;

            int number = Random.Range(0, exitObjects.Length);
            exitObjects[number].SetActive(true);
            exitObjects[number].GetComponent<DoorOpener>().OpenDoor();

            // ゴールマーカーを扉の上空に生成する
            Vector3 goalPosition = new Vector3(exitObjects[number].transform.position.x, 20f, exitObjects[number].transform.position.z);
            MapManager.Instance.GoalMarkerCreate(goalPosition);

            foreach (GameObject f in flag)
            {
                f.SetActive(true);
            }

            StartCoroutine(PlayExitCinematic(number));
        }
    }

    // 開いた扉を映すカメラ演出。演出中は Time.timeScale = 0 でゲームを止め、実時間で待つ
    private IEnumerator PlayExitCinematic(int exitIndex)
    {
        exitCameras[exitIndex].enabled = true;
        playerCamera.enabled = false;

        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(cinematicDuration);

        exitCameras[exitIndex].enabled = false;
        playerCamera.enabled = true;

        Time.timeScale = 1f;
    }
}