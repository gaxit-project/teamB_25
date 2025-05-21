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
        bgmSlider.value = PlayerPrefs.GetFloat("BGM_VOLUME", 0.5f);
        seSlider.value = PlayerPrefs.GetFloat("SE_VOLUME", 0.5f);

        bgmSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
        seSlider.onValueChanged.AddListener(AudioManager.Instance.SetSEVolume);
    }
}
