using System.Collections;
using UnityEngine;

/// <summary>
/// 導入シーンのカメラ切り替え演出。カメラを順番に切り替え、一周したら(またはスキップ入力で)
/// Main シーンへ遷移する。
/// 連携: AudioManager(導入SE), SceneChangeManager
/// </summary>
public class IntroductionCamera : MonoBehaviour
{
    [Tooltip("切り替え順に並べたカメラ")]
    public Camera[] cameras;

    public float switchInterval = 3f;
    private int currentIndex = 0;
    private float timer = 0f;
    private bool initialized = false;

    // 最初のカメラだけを表示し、導入SEを再生する。二重初期化は防ぐ
    public void InitializeIntro()
    {
        if (initialized) return;
        initialized = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE("Radio", transform.position);
        }

        if (cameras == null || cameras.Length == 0)
        {
            enabled = false;
            return;
        }

        for (int i = 0; i < cameras.Length; i++)
            cameras[i].enabled = (i == 0);

        currentIndex = 0;
        timer = 0f;
    }

    private void OnEnable()
    {
        Time.timeScale = 1f;
        StartCoroutine(DelayedInitializeIntro());
    }

    // 他のオブジェクトの初期化が済むよう、1フレーム待ってから初期化する
    private IEnumerator DelayedInitializeIntro()
    {
        yield return null;

        InitializeIntro();
    }

    void Update()
    {
        // スキップ入力(Space / ゲームパッドのボタン7)。再生中の導入SEは
        // AudioManager の子オブジェクトとして存在するので、削除して止めてから遷移する
        if (Input.GetKeyDown("joystick button 7") || Input.GetKeyDown(KeyCode.Space))
        {
            if (AudioManager.Instance != null)
            {
                foreach (Transform child in AudioManager.Instance.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            SceneChangeManager.Instance.ChangeScene("Main");
            return;
        }

        if (cameras == null || cameras.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= switchInterval)
        {
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