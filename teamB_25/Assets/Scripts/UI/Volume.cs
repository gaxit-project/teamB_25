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
        float bgm = PlayerPrefs.GetFloat(AudioKey.BGM_VOLUME_KEY, 0.5f);
        float se = PlayerPrefs.GetFloat(AudioKey.SE_VOLUME_KEY, 0.5f);

        // スライダーを保存済みの音量に合わせ、変更時にAudioManagerへ反映する
        bgmSlider.value = bgm;
        seSlider.value = se;
        AudioManager.Instance.SetBGMVolume(bgm);
        AudioManager.Instance.SetSEVolume(se);

        bgmSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        seSlider.onValueChanged.AddListener(AudioManager.Instance.SetSEVolume);
    }
}
