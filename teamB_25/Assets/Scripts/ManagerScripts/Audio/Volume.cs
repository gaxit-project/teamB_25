using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Volume : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider seSlider;

    private void Start()
    {
        float bgm = PlayerPrefs.GetFloat("BGM_VOLUME", 0.5f);
        float se = PlayerPrefs.GetFloat("SE_VOLUME", 0.5f);

        bgmSlider.value = bgm;
        seSlider.value = se;

        // AudioManagerに反映（←これがなかった）
        AudioManager.Instance.SetBGMVolume(bgm);
        AudioManager.Instance.SetSEVolume(se);

        // スライダーのイベント登録
        bgmSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        seSlider.onValueChanged.AddListener(AudioManager.Instance.SetSEVolume);
    }
}
