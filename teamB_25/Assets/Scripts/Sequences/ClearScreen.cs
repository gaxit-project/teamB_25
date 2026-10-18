using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// クリア演出。カバー画像を徐々に透明にし、alpha が 0.2 を下回ったらクリア処理を一度だけ実行して、
/// 音を止めてからタイトルへ戻る。
/// </summary>
public class ClearScreen : MonoBehaviour
{
    public Image myImage;

    // 1秒あたりの alpha 減少量。フレームレートに依存しないよう時間基準にしている
    [SerializeField] private float fadeSpeed = 0.06f;

    private float alpha;
    private bool isCleared = false; // クリア処理が一度だけ走るように制御

    // 旧仕様(クリア後にボタンで選択)の名残。現在は自動でタイトルへ戻るため、
    // ボタンは操作できないまま非表示にしている
    [Header("Clear後に操作させたいボタン")]
    public Button titleButton;
    public Button exitButton;

    [Header("ボタン背景用のImage")]
    public Image titleButtonBG;
    public Image exitButtonBG;

    private void Start()
    {
        alpha = 1.0f;

        if (titleButton != null) titleButton.interactable = false;
        if (exitButton != null) exitButton.interactable = false;

        if (titleButtonBG != null) titleButtonBG.enabled = false;
        if (exitButtonBG != null) exitButtonBG.enabled = false;
    }

    private void Update()
    {
        if (alpha >= 0.2f)
        {
            ChangeImage();
            return;
        }

        if (isCleared) return;
        isCleared = true;

        Destroy(myImage);
        OnGameClear();
    }

    public void ChangeImage()
    {
        alpha -= fadeSpeed * Time.deltaTime;
        Color color = myImage.color;
        color.a = alpha;
        myImage.color = color;
    }

    private void OnGameClear()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
            AudioManager.Instance.StopAllSELoops();

            // 再生中のワンショットSEも止める。BGM用AudioSourceは StopBGM() で止まっているので除外
            foreach (var source in FindObjectsOfType<AudioSource>())
            {
                if (source != AudioManager.Instance.bgmSource)
                {
                    source.Stop();
                }
            }
        }

        Invoke(nameof(TitleGO), 3f);
    }

    private void TitleGO()
    {
        SceneManager.LoadScene("Title");
    }
}