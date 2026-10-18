using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ポーズの管理。Time.timeScale と AudioListener.pause でゲームを止め、ポーズ画面を表示する。
/// ポーズ状態は Time.timeScale で判定する(0 = ポーズ中)。
/// 連携: InputManager(入力を受けて Pause を呼ぶ), PauseLoad(未配置のシーンで生成),
///       AudioManager, SceneChangeManager
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    public Canvas PauseCanvas;
    [SerializeField] Button focusButton; // ポーズ画面を開いたときに選択するボタン
    [SerializeField] Button StartButton; // ポーズ解除時に選択状態へ戻すボタン
    [SerializeField] CanvasGroup PauseCG;
    [SerializeField] CanvasGroup otherCG; // ポーズ中は操作を無効にする、ゲーム側のUI

    public bool CountDown; // true の間はポーズ画面を開かない

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetOtherCG(CanvasGroup cg)
    {
        otherCG = cg;
    }

    // シーン遷移後はポーズ状態を必ず解除する
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PauseCanvas.enabled = false;
    }

    void Start()
    {
        if (otherCG != null)
        {
            otherCG.interactable = true;
        }
        if (SceneManager.GetActiveScene().name == "Title")
        {
            PauseCG.interactable = false;
        }
    }

    // ポーズ画面のボタンから呼ばれるシーン遷移。ポーズ状態を戻してから遷移する
    public void ChangeScene(string _sceneName)
    {
        Time.timeScale = 1.0f;
        PauseCanvas.gameObject.SetActive(false);
        AudioManager.Instance.ResumeAudio();
        SceneChangeManager.Instance.ChangeScene(_sceneName);
    }

    // ポーズの開始 / 解除を切り替える
    public void Pause()
    {
        Time.timeScale = (Time.timeScale == 0f) ? 1f : 0f;
        if (Time.timeScale == 0f)
        {
            if (!CountDown)
            {
                PauseNow();
            }
        }
        else
        {
            PauseCG.interactable = false;
            AudioManager.Instance.ResumeAudio();

            PauseCanvas.enabled = false;
            if (otherCG != null)
            {
                otherCG.interactable = true;
                StartButton.Select();
            }
        }
    }

    private void PauseNow()
    {
        AudioManager.Instance.StopAllSELoops();
        AudioManager.Instance.PauseAudio();
        PauseCanvas.enabled = true;
        PauseCG.interactable = true;
        if (otherCG != null)
        {
            otherCG.interactable = false;
        }
        focusButton.Select();
    }
}