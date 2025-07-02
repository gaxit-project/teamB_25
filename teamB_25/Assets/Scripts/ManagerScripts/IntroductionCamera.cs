using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.SceneManagement;

public class IntroductionCamera : MonoBehaviour
{
    [Tooltip("切り替え順に並べたカメラ")]
    public Camera[] cameras;

    public float switchInterval = 3f;
    private int currentIndex = 0;
    private float timer = 0f;
    private bool initialized = false;

    /// <summary>
    ///  初期化
    /// </summary>
    public void InitializeIntro()
    {
        Debug.Log($"InitializeIntro called. timer={timer}, currentIndex={currentIndex}, initialized={initialized}");


        if (initialized) return; // 初期化済みなら再初期化を防ぐ

        Debug.Log("Intro初期化開始");

        AudioManager.Instance.PlaySE("Radio", transform.position);
        Debug.Log("SE");

        initialized = true;

        if (cameras == null || cameras.Length == 0)
        {
            enabled = false;
            return;
        }

        for (int i = 0; i < cameras.Length; i++)
            cameras[i].enabled = (i == 0);　// 最初のカメラをオン

        currentIndex = 0; // カメラをリセット
        timer = 0f;　//　タイマーをリセット

        initialized = true;
    }

    private void OnEnable()
    {
        Time.timeScale = 1f;
        StartCoroutine(DelayedInitializeIntro());
    }

    private IEnumerator DelayedInitializeIntro()
    {
        yield return null;  // 1フレーム待つ

        InitializeIntro();   // ここでSE再生などの初期化を呼ぶ
    }



    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("joystick button 7") || Input.GetKeyDown(KeyCode.Space))
        {
            // 既存の SE オブジェクトを削除
            foreach (Transform child in AudioManager.Instance.transform)
            {
                Destroy(child.gameObject);
            }
            SceneChangeManager.Instance.ChangeScene("Main");
        }

        timer += Time.deltaTime;


        if (timer >= switchInterval)
        {
            // 現在のカメラをOFF
            cameras[currentIndex].enabled = false;

            // 次のカメラに切り替え(ループ)
            currentIndex = (currentIndex + 1) % cameras.Length;
            cameras[currentIndex].enabled = true;

            timer = 0f;

            // 一周終わったらシーン移動
            if (currentIndex == 0)
            {
                SceneChangeManager.Instance.ChangeScene("Main");
            }
        }
    }
}
