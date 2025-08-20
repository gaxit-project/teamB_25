using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClearScreen : MonoBehaviour
{
    public Image myImage;
    private float alpha;
    private bool isCleared = false; // クリア処理が一度だけ走るように制御

    private void Start()
    {
        alpha = 1.0f;
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

        Debug.Log("Game Cleared! All Audio stopped, AudioManager kept alive for restart.");
    }
}
