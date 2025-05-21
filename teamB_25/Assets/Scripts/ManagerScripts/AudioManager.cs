using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NamedAudioClip
{
    public string name;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sePrefab;

    [Header("BGM Clips")]
    public List<NamedAudioClip> bgmClips;

    [Header("SE Clips")]
    public List<NamedAudioClip> seClips;

    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> seDict = new Dictionary<string, AudioClip>();

    private const string BGM_VOLUME_KEY = "BGM_VOLUME";
    private const string SE_VOLUME_KEY = "SE_VOLUME";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadClips();
            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadClips()
    {
        foreach (var named in bgmClips)
            bgmDict[named.name] = named.clip;

        foreach (var named in seClips)
            seDict[named.name] = named.clip;
    }

    void LoadVolumeSettings()
    {
        float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        float seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);

        SetBGMVolume(bgmVolume);
        SetSEVolume(seVolume);
    }

    public void PlayBGM(string name)
    {
        if (bgmDict.TryGetValue(name, out var clip))
        {
            if (bgmSource.clip != clip)
            {
                bgmSource.clip = clip;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("BGM not found: " + name);
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlaySE(string name)
    {
        if (seDict.TryGetValue(name, out var clip))
        {
            AudioSource se = Instantiate(sePrefab, transform);
            se.clip = clip;
            se.volume = sePrefab.volume;
            se.Play();
            Destroy(se.gameObject, clip.length);
        }
        else
        {
            Debug.LogWarning("SE not found: " + name);
        }
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, volume);
    }

    public void SetSEVolume(float volume)
    {
        sePrefab.volume = volume;
        PlayerPrefs.SetFloat(SE_VOLUME_KEY, volume);
    }
}
