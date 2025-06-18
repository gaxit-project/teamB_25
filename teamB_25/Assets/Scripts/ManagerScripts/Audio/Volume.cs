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

        bgmSlider.value = bgm;
        seSlider.value = se;

        // AudioManager�ɔ��f�i�����ꂪ�Ȃ������j
        AudioManager.Instance.SetBGMVolume(bgm);
        AudioManager.Instance.SetSEVolume(se);

        // �X���C�_�[�̃C�x���g�o�^
        bgmSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        seSlider.onValueChanged.AddListener(AudioManager.Instance.SetSEVolume);
    }
}
