using System.Collections;
using UnityEngine;

public class BreakerManager : MonoBehaviour
{
    public static BreakerManager Instance;
    public static int _breakerTask = 4;
    public static int _breakerOn = 0;
    private bool exitOpened = false;
    public GameObject[] exitObjects;

    public Camera[] exitCameras; // 脱出扉ごとのカメラ
    public Camera playerCamera;　// プレイヤーごとのカメラ
    public float cinematicDuration = 2f;

    private void Awake()
    {
        Instance = this; //シングルトンにする
    }

    // Start is called before the first frame update
    void Start()
    {
        // 非アクティブにする
        foreach (GameObject obj in exitObjects)
        {
            obj .SetActive(false);
        }
        // 全カメラオフ
        playerCamera.enabled = true;
        foreach (Camera cam in exitCameras)
            cam.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O)) // デバッグ用:Oキーで扉の開放条件を満たす
        {
            _breakerOn = _breakerTask - 1;
            ActivateBreaker();
        }
    }

    public void ActivateBreaker()
    {
        _breakerOn++;

        // playerが電力復旧出来たら
        if (!exitOpened && _breakerOn == _breakerTask)
        {
            exitOpened = true; // アクティブ状態にする

            int number = Random.Range(0, exitObjects.Length);  // ランダムで１つ選択
            Debug.Log($"脱出扉{number}が開きました");
            exitObjects[number].SetActive(true);
            exitObjects[number].GetComponent<DoorOpener>().OpenDoor();

            StartCoroutine(PlayExitCinematic(number));
        }
    }

    private IEnumerator PlayExitCinematic(int exitIndex)
    {
        // 対象カメラON
        exitCameras[exitIndex].enabled = true;
        playerCamera.enabled = false;

        // 時間停止（または制御停止用のフラグ管理）
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(cinematicDuration);

        // カメラ戻す
        exitCameras[exitIndex].enabled = false;
        playerCamera.enabled = true;

        // 時間再開
        Time.timeScale = 1f;
    }
}
