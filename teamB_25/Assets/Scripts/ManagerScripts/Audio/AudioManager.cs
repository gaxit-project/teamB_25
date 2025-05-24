using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

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

    [Header("SELoop Clips")]
    public List<NamedAudioClip> seLoopClips;
    //é´èë
    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> seDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> seLoopDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioSource> activeLoops = new Dictionary<string, AudioSource>();



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
    /// <summary>
    /// ñºëOÇ≈åüçıÇ≈Ç´ÇÈÇÊÇ§Ç…Ç∑ÇÈ
    /// </summary>
    void LoadClips()
    {
        foreach (var named in bgmClips)
            bgmDict[named.name] = named.clip;

        foreach (var named in seClips)
            seDict[named.name] = named.clip;

        foreach (var named in seLoopClips)
            seLoopDict[named.name] = named.clip;
    }
    /// <summary>
    /// ï€ë∂ÇµÇΩâπó Çì«Ç›çûÇ›îΩâfÇ≥ÇπÇÈ
    /// </summary>
    void LoadVolumeSettings()
    {
        float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        float seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);


        SetBGMVolume(bgmVolume);
        SetSEVolume(seVolume);
    }
    /// <summary>
    /// BGMñ¬ÇÁÇ∑
    /// </summary>
    /// <param name="name"></param>
    public void PlayBGM(string name)
    {
        if (bgmDict.TryGetValue(name, out var clip))
        {
            //êVÇµÇ¢Ç‡ÇÃÇÃèÍçá
            if (bgmSource.clip != clip)
            {
                bgmSource.clip = clip;
                bgmSource.loop = true;
            }
            //çƒê∂ÇµÇƒÇ¢Ç»Ç©Ç¡ÇΩÇÁ
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
        else
        {
            Debug.Log("BGMÇ™Ç»Ç¢");
        }
    }
    /// <summary>
    /// BGMÇé~ÇﬂÇÈ
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }
    /// <summary>
    /// SEÇÇ»ÇÁÇ∑
    /// </summary>
    /// <param name="name"></param>
    public void PlaySE(string name,Vector3 position)
    {
        if (seDict.TryGetValue(name, out var clip))
        {
            AudioSource se = Instantiate(sePrefab, position,Quaternion.identity,transform);
            se.clip = clip;
            se.volume = sePrefab.volume;
            se.Play();
            Destroy(se.gameObject, clip.length);
        }
        else
        {
            Debug.Log("SEÇ™Ç»Ç¢");
        }
    }
    /// <summary>
    /// LoopêÍópSE
    /// </summary>
    /// <param name="name"></param>
    public void PlaySELoop(string name, Transform target)
    {
        if(seLoopDict.TryGetValue(name, out var clip))
        {
            // Ç∑Ç≈Ç…çƒê∂íÜÇ»ÇÁé~ÇﬂÇÈ
            if (activeLoops.ContainsKey(name))
            {
                Destroy(activeLoops[name]);
                activeLoops.Remove(name);
            }

            AudioSource seLoop = target.gameObject.AddComponent<AudioSource>();
            seLoop.clip = clip;
            seLoop.volume = sePrefab.volume;
            seLoop.spatialBlend = 1f;
            seLoop.loop = true;
            seLoop.Play();
            activeLoops[name] = seLoop;
        }
    }
    /// <summary>
    /// LoopÇµÇƒÇ¢ÇÈSEÇçÌèúÇ∑ÇÈ
    /// </summary>
    /// <param name="name"></param>
    public void DestroySE(string name)
    {
        if (activeLoops.TryGetValue(name, out var source))
        {
            source.Stop();
            Destroy(source);
            activeLoops.Remove(name);
        }
    }
    /// <summary>
    /// âπó ï€ë∂
    /// </summary>
    /// <param name="volume"></param>
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
