using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ClearScreen : MonoBehaviour
{
    public Image myImage;
    private float alpha;
    private bool isCleared = false; // クリア処理が一度だけ走るように制御

    [Header("Clear後に操作させたいボタン")]
    public Button titleButton;
    public Button exitButton;

    [Header("ボタン背景用のImage")]
    public Image titleButtonBG;
    public Image exitButtonBG;

    private void Start()
    {
        alpha = 1.0f;

        // 最初はボタンを無効化
        if (titleButton != null) titleButton.interactable = false;
        if (exitButton != null) exitButton.interactable = false;

        // 最初は背景も非表示
        if (titleButtonBG != null) titleButtonBG.enabled = false;
        if (exitButtonBG != null) exitButtonBG.enabled = false;
    }

    private void Update()
    {
        if (alpha < 0.2f)
        {
            Destroy(myImage);

            if (!isCleared)
            {
                OnGameClear();
                isCleared = true;
            }
        }
        else
        {
            ChangeImage();
        }
    }

    public void ChangeImage()
    {
        alpha -= 0.001f;
        Color color = myImage.color;
        color.a = alpha;
        myImage.color = color;
    }

    private void OnGameClear()
    {
        if (AudioManager.Instance != null)
        {
            // BGM 停止
            AudioManager.Instance.StopBGM();

            // ループSE全部停止
            AudioManager.Instance.StopAllSELoops();

            // 再生中のワンショットSEも停止
            foreach (var source in FindObjectsOfType<AudioSource>())
            {
                // BGM用AudioSourceは StopBGM() で止まっているので除外
                if (source != AudioManager.Instance.bgmSource)
                {
                    source.Stop();
                }
            }
        }

        Invoke("TitleGO", 3f);
        //// フェード完了後にボタンを有効化
        //if (titleButton != null)
        //{
        //    titleButton.interactable = true;
        //    if (titleButtonBG != null) titleButtonBG.enabled = true;
        //}
        //if (exitButton != null)
        //{
        //    exitButton.interactable = true;
        //    if (exitButtonBG != null) exitButtonBG.enabled = true;
        //}

        //// 最初にフォーカスするボタンを指定（InputManagerで操作するため）
        //if (titleButton != null)
        //{
        //    EventSystem.current.SetSelectedGameObject(titleButton.gameObject);
        //}
    }
    void TitleGO()
    {
        SceneManager.LoadScene("Title");
    }
}
