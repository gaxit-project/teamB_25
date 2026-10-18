using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NamedAudioClip
{
    public string name;
    public AudioClip clip;
}

// PlayerPrefs の保存キー
class AudioKey
{
    public const string BGM_VOLUME_KEY = "BGM_VOLUME";
    public const string SE_VOLUME_KEY = "SE_VOLUME";
}

// SE名の定数。Inspector の NamedAudioClip.name と一致させる
class AudioDefine
{
    public const string PlayerRun = "PlayerRun";
    public const string PlayerWalk = "PlayerWalk";
    public const string Walk = "Walk";
    public const string Dash = "Dash";
}

/// <summary>
/// BGM / SE の再生を一元管理するシングルトン。シーンをまたいで残る(DontDestroyOnLoad)。
/// クリップは名前で引けるように Dictionary 化し、呼び出し側は文字列名だけで再生できる。
/// 音量は PlayerPrefs に保存し、起動時に復元する。
/// </summary>
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

    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> seDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> seLoopDict = new Dictionary<string, AudioClip>();

    // 再生中のループSE。(SE名, 対象のInstanceID) をキーにして、同じ対象での二重再生を防ぐ
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
            // シーン再読み込みで重複生成された場合は新しい方を破棄する
            Destroy(gameObject);
        }
    }

    public void PauseAudio()
    {
        AudioListener.pause = true;
    }

    public void ResumeAudio()
    {
        AudioListener.pause = false;
    }

    // Inspector のリストを名前で検索できる Dictionary に変換する
    void LoadClips()
    {
        foreach (var named in bgmClips)
            bgmDict[named.name] = named.clip;

        foreach (var named in seClips)
            seDict[named.name] = named.clip;

        foreach (var named in seLoopClips)
            seLoopDict[named.name] = named.clip;
    }

    // 保存済みの音量を読み込んで反映する
    void LoadVolumeSettings()
    {
        float bgmVolume = PlayerPrefs.GetFloat(AudioKey.BGM_VOLUME_KEY, 0.5f);
        float seVolume = PlayerPrefs.GetFloat(AudioKey.SE_VOLUME_KEY, 0.5f);

        SetBGMVolume(bgmVolume);
        SetSEVolume(seVolume);
    }

    public void PlayBGM(string name)
    {
        // ポーズ中(timeScale = 0)は再生しない
        if (Time.deltaTime == 0)
        {
            return;
        }
        if (bgmDict.TryGetValue(name, out var clip))
        {
            if (bgmSource.clip != clip)
            {
                bgmSource.clip = clip;
                bgmSource.loop = true;
            }
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // ワンショットSE。指定位置に3D音源を生成し、再生後に破棄する
    public void PlaySE(string name, Vector3 position)
    {
        if (Time.deltaTime == 0)
        {
            return;
        }
        if (seDict.TryGetValue(name, out var clip))
        {
            AudioSource se = Instantiate(sePrefab, position, Quaternion.identity, transform);
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
            Debug.LogWarning($"AudioManager: SE not found: {name}");
        }
    }

    // ループSE(足音など)。対象オブジェクトに直接 AudioSource を付けて、移動に音源を追従させる
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

            // 既に再生中なら何もしない。対象が破棄済みで記録だけ残っている場合は鳴らし直す
            if (activeLoops.TryGetValue(key, out var existing) && existing != null)
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

    // ループSEを停止し、付けた AudioSource を削除する
    public void DestroySE(string name, Transform target)
    {
        int instanceId = target.gameObject.GetInstanceID();
        var key = (name, instanceId);
        if (activeLoops.TryGetValue(key, out var source))
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source);
            }
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