using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

[System.Serializable]
public class NamedAudioClip
{
    public string name;
    public AudioClip clip;
}
class AudioKey
{
    public static string BGM_VOLUME_KEY = "BGM_VOLUME";
    public static string SE_VOLUME_KEY = "SE_VOLUME";
}

class AudioDefine
{
    public static string PlayerRun = "PlayerRun";
    public static string PlayerWalk = "PlayerWalk";
    public static string Walk = "Walk";
    public static string Dash = "Dash";
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
    private Dictionary<(string, int), AudioSource> activeLoops = new Dictionary<(string, int), AudioSource>();





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
    private void Update()
    {

    }

    public void PauseAudio()
    {
        AudioListener.pause = true;
    }

    public void ResumeAudio()
    {
        AudioListener.pause = false;
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
        float bgmVolume = PlayerPrefs.GetFloat(AudioKey.BGM_VOLUME_KEY, 0.5f);
        float seVolume = PlayerPrefs.GetFloat(AudioKey.SE_VOLUME_KEY, 0.5f);


        SetBGMVolume(bgmVolume);
        SetSEVolume(seVolume); 
    }
    /// <summary>
    /// BGM�炷
    /// </summary>
    /// <param name="name"></param>
    public void PlayBGM(string name)
    {
        if (Time.deltaTime == 0)
        {
            return;
        }
        if (bgmDict.TryGetValue(name, out var clip))
        {
            //�V������̂̏ꍇ
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
    /// BGM��~�߂�
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }
    /// <summary>
    /// SE��Ȃ炷
    /// </summary>
    /// <param name="name"></param>
    public void PlaySE(string name,Vector3 position)
    {
        if (Time.deltaTime == 0)
        {
            return;
        }
        if (seDict.TryGetValue(name, out var clip))
        {
            AudioSource se = Instantiate(sePrefab, position,Quaternion.identity,transform);
            se.clip = clip;
            se.volume = sePrefab.volume;
            se.minDistance = 3f;
            se.maxDistance = 60f;
            se.spatialBlend = 1f;
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
        if (Time.deltaTime == 0)
        {
            return;
        }
        if (seLoopDict.TryGetValue(name, out var clip))
        {
            int instanceId = target.gameObject.GetInstanceID();
            var key = (name, instanceId);
            // ���łɍĐ����Ȃ�~�߂�
            if (activeLoops.ContainsKey(key))
            {
                return;
            }

            AudioSource seLoop = target.gameObject.AddComponent<AudioSource>();
            seLoop.clip = clip;
            seLoop.volume = sePrefab.volume;
            seLoop.minDistance = 3f;
            seLoop.maxDistance = 60f;
            seLoop.spatialBlend = 1f;
            seLoop.loop = true;
            seLoop.Play();
            activeLoops[key] = seLoop;
        }
    }
    /// <summary>
    /// Loop���Ă���SE��폜����
    /// </summary>
    /// <param name="name"></param>
    public void DestroySE(string name,Transform target)
    {
        int instanceId = target.gameObject.GetInstanceID();
        var key = (name, instanceId);
        if (activeLoops.TryGetValue(key, out var source))
        {
            source.Stop();
            Destroy(source);
            activeLoops.Remove(key);
        }
    }

    public void StopAllSELoops()
    {
        foreach (var source in activeLoops.Values)
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source);
                
            }
        }
        activeLoops.Clear();
    }
    /// <summary>
    /// ���ʕۑ�
    /// </summary>
    /// <param name="volume"></param>
    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
        PlayerPrefs.SetFloat(AudioKey.BGM_VOLUME_KEY, volume);
    }

    public void SetSEVolume(float volume)
    {
        sePrefab.volume = volume;
        PlayerPrefs.SetFloat(AudioKey.SE_VOLUME_KEY, volume);
    }
}
