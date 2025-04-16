using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    //SE�̓����Đ��̃`�����l��
    private const int SE_CHANNEL = 4;

    //����
    public static float SEVolume;
    public static float BgmVolume;

    //�T�E���h���
    private enum soundType
    {
        Bgm,
        SE,
    }

    //�V���O���g��
    private static SoundManager _singleton = new SoundManager();

    //�C���X�^���X�擾
    private static SoundManager GetInstanse()
    {
        return _singleton ?? (_singleton = new SoundManager());
    }

    //�T�E���h�Đ��p�̋�̃Q�[���I�u�W�F�N�g
    private GameObject _gameObject;

    //�T�E���h���\�[�X
    private AudioSource _bgmSource = null;
    private AudioSource _SESourceDefault = null;
    private AudioSource[] _SESourceArray;

    //BGM�ɃA�N�Z�X����p�̃f�[�^�e�[�u��
    private Dictionary<string, _Data> _poolBgm = new Dictionary<string, _Data>();
    //SE�ɃA�N�Z�X����p�̃f�[�^�e�[�u��
    private Dictionary<string, _Data> _poolSE = new Dictionary<string, _Data>();

    class _Data
    {
        //�A�N�Z�X�L�[
        public string Key;
        //�t�@�C���̖��O
        public string Name;
        //�I�[�f�B�I�N���b�v
        public AudioClip Clip;
        //���̒���
        public float Duration;

        //�R���X�g���N�^
        public _Data(string key, string name)
        {
            this.Key = key;
            this.Name = "Sounds/" + name;

            //AudioClip�擾
            Clip = Resources.Load(Name) as AudioClip;
            //���̒����̎擾
            Duration = Clip.length;
        }

    }

    //�R���X�g���N�^
    public SoundManager()
    {
        //�`�����l���̊m��
        _SESourceArray = new AudioSource[SE_CHANNEL];
    }

    private AudioSource GetAudioSource(soundType type, int channel = -1)
    {
        if (_gameObject == null)
        {
            //gameObject���Ȃ��Ƃ��A�V���ɍ쐬����
            _gameObject = new GameObject("SoundManager");

            //�j���ł��Ȃ��悤��
            GameObject.DontDestroyOnLoad(_gameObject);

            //AudioSource�̍쐬
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
                // �`�����l���w��
                return _SESourceArray[channel];
            }
            else
            {
                // �f�t�H���g
                return _SESourceDefault;
            }
        }
    }

    //�T�E���h�̃��[�h
    //�K:Resources/Sounds�Ƀf�[�^�̔z�u�AResources��CSV�ɒǋL
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
            //�o�^�ς݂̏ꍇ�A�X�V�̂��ߑO�f�[�^���폜����
            _poolBgm.Remove(key);
        }
        _poolBgm.Add(key, new _Data(key, name));
    }

    private void LoadSEInternal(string key, string name)
    {
        if (_poolSE.ContainsKey(key))
        {
            //�o�^�ς݂̏ꍇ�A�X�V�̂��ߑO�f�[�^���폜����
            _poolSE.Remove(key);
        }
        _poolSE.Add(key, new _Data(key, name));
    }

    //BGM�Đ�
    //�K:LoadBgm
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

        //�T�E���h�̒�~
        StopBgmInternal();

        //�T�E���h���\�[�X�擾
        var _data = _poolBgm[key];

        //�Đ�
        var source = GetAudioSource(soundType.Bgm);
        source.loop = true;
        source.clip = _data.Clip;
        source.volume = BgmVolume;
        source.Play();

        return true;
    }

    //BGM��~
    public static void StopBgm()
    {
        GetInstanse().StopBgmInternal();
    }
    private void StopBgmInternal()
    {
        GetAudioSource(soundType.Bgm).Stop();
    }


    //SE�Đ�
    //�K:LoadSE

    private bool PlaySEInternal(string key, int channel = -1)
    {
        if (!_poolSE.ContainsKey(key))
        {
            return false;
        }

        //�T�E���h���\�[�X�擾
        var _data = _poolSE[key];

        if (0 <= channel && channel < SE_CHANNEL)
        {
            //channel�̎w��
            var source = GetAudioSource(soundType.SE, channel);
            source.clip = _data.Clip;
            source.volume = SEVolume;
            source.Play();
        }
        else
        {
            //�f�t�H���g�Đ�
            var source = GetAudioSource(soundType.SE);
            source.PlayOneShot(_data.Clip, SEVolume);
        }

        return true;
    }
}
