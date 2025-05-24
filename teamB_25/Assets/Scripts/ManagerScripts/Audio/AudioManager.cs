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
    //����
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
    /// ���O�Ō����ł���悤�ɂ���
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
    /// �ۑ��������ʂ�ǂݍ��ݔ��f������
    /// </summary>
    void LoadVolumeSettings()
    {
        float bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        float seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);


        SetBGMVolume(bgmVolume);
        SetSEVolume(seVolume);
    }
    /// <summary>
    /// BGM�炷
    /// </summary>
    /// <param name="name"></param>
    public void PlayBGM(string name)
    {
        if (bgmDict.TryGetValue(name, out var clip))
        {
            //�V�������̂̏ꍇ
            if (bgmSource.clip != clip)
            {
                bgmSource.clip = clip;
                bgmSource.loop = true;
            }
            //�Đ����Ă��Ȃ�������
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
        else
        {
            Debug.Log("BGM���Ȃ�");
        }
    }
    /// <summary>
    /// BGM���~�߂�
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }
    /// <summary>
    /// SE���Ȃ炷
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
            Debug.Log("SE���Ȃ�");
        }
    }
    /// <summary>
    /// Loop��pSE
    /// </summary>
    /// <param name="name"></param>
    public void PlaySELoop(string name, Transform target)
    {
        if(seLoopDict.TryGetValue(name, out var clip))
        {
            // ���łɍĐ����Ȃ�~�߂�
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
    /// Loop���Ă���SE���폜����
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
    /// ���ʕۑ�
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
