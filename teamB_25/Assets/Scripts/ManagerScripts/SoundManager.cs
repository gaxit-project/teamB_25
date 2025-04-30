using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    //SEの同時再生のチャンネル
    private const int SE_CHANNEL = 4;

    //音量
    public static float SEVolume;
    public static float BgmVolume;

    //サウンド種別
    private enum soundType
    {
        Bgm,
        SE,
    }

    //シングルトン
    private static SoundManager _singleton = new SoundManager();

    //インスタンス取得
    private static SoundManager GetInstanse()
    {
        return _singleton ?? (_singleton = new SoundManager());
    }

    //サウンド再生用の空のゲームオブジェクト
    private GameObject _gameObject;

    //サウンドリソース
    private AudioSource _bgmSource = null;
    private AudioSource _SESourceDefault = null;
    private AudioSource[] _SESourceArray;

    //BGMにアクセスする用のデータテーブル
    private Dictionary<string, _Data> _poolBgm = new Dictionary<string, _Data>();
    //SEにアクセスする用のデータテーブル
    private Dictionary<string, _Data> _poolSE = new Dictionary<string, _Data>();

    class _Data
    {
        //アクセスキー
        public string Key;
        //ファイルの名前
        public string Name;
        //オーディオクリップ
        public AudioClip Clip;
        //音の長さ
        public float Duration;

        //コンストラクタ
        public _Data(string key, string name)
        {
            this.Key = key;
            this.Name = "Sounds/" + name;

            //AudioClip取得
            Clip = Resources.Load(Name) as AudioClip;
            //音の長さの取得
            Duration = Clip.length;
        }

    }

    //コンストラクタ
    public SoundManager()
    {
        //チャンネルの確保
        _SESourceArray = new AudioSource[SE_CHANNEL];
    }

    private AudioSource GetAudioSource(soundType type, int channel = -1)
    {
        if (_gameObject == null)
        {
            //gameObjectがないとき、新たに作成する
            _gameObject = new GameObject("SoundManager");

            //破棄できないように
            GameObject.DontDestroyOnLoad(_gameObject);

            //AudioSourceの作成
            _bgmSource = _gameObject.AddComponent<AudioSource>();
            _SESourceDefault = _gameObject.AddComponent<AudioSource>();
            for (int i = 0; i < SE_CHANNEL; i++)
            {
                _SESourceArray[i] = _gameObject.AddComponent<AudioSource>();
            }
        }

        if (type == soundType.Bgm)
        {
            // BGM
            return _bgmSource;
        }
        else
        {
            // SE
            if (0 <= channel && channel < SE_CHANNEL)
            {
                // チャンネル指定
                return _SESourceArray[channel];
            }
            else
            {
                // デフォルト
                return _SESourceDefault;
            }
        }
    }

    //サウンドのロード
    //必:Resources/Soundsにデータの配置、ResourcesのCSVに追記
    public static void LoadBgm(string key, string name)
    {
        GetInstanse().LoadBgmInternal(key, name);
    }
    public static void LoadSE(string key, string name)
    {
        GetInstanse().LoadSEInternal(key, name);
    }


    private void LoadBgmInternal(string key, string name)
    {
        if (_poolBgm.ContainsKey(key))
        {
            //登録済みの場合、更新のため前データを削除する
            _poolBgm.Remove(key);
        }
        _poolBgm.Add(key, new _Data(key, name));
    }

    private void LoadSEInternal(string key, string name)
    {
        if (_poolSE.ContainsKey(key))
        {
            //登録済みの場合、更新のため前データを削除する
            _poolSE.Remove(key);
        }
        _poolSE.Add(key, new _Data(key, name));
    }

    //BGM再生
    //必:LoadBgm
    public static bool PlayBgm(string key)
    {
        return GetInstanse().PlayBgmInternal(key);
    }
    private bool PlayBgmInternal(string key)
    {
        if (!_poolBgm.ContainsKey(key))
        {
            return false;
        }

        //サウンドの停止
        StopBgmInternal();

        //サウンドリソース取得
        var _data = _poolBgm[key];

        //再生
        var source = GetAudioSource(soundType.Bgm);
        source.loop = true;
        source.clip = _data.Clip;
        source.volume = BgmVolume;
        source.Play();

        return true;
    }

    //BGM停止
    public static void StopBgm()
    {
        GetInstanse().StopBgmInternal();
    }
    private void StopBgmInternal()
    {
        GetAudioSource(soundType.Bgm).Stop();
    }


    //SE再生
    //必:LoadSE

    private bool PlaySEInternal(string key, int channel = -1)
    {
        if (!_poolSE.ContainsKey(key))
        {
            return false;
        }

        //サウンドリソース取得
        var _data = _poolSE[key];

        if (0 <= channel && channel < SE_CHANNEL)
        {
            //channelの指定
            var source = GetAudioSource(soundType.SE, channel);
            source.clip = _data.Clip;
            source.volume = SEVolume;
            source.Play();
        }
        else
        {
            //デフォルト再生
            var source = GetAudioSource(soundType.SE);
            source.PlayOneShot(_data.Clip, SEVolume);
        }

        return true;
    }
}
