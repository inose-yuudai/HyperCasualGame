using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; // ★★★ DOTweenの名前空間を追加

// BGMの種類を管理するenum
public enum BGMType
{
    None, // BGMを再生しない場合
    Gameplay
}

// SFXの種類を管理するenum（既存）
public enum SFXType
{
    PlayerDamage,
    EnemyDamage,
    EnemyDeath,
    PlayerDeath,
    BombExplosion,
    stretchBone,
    CahnegeRotation,
    ClockTick
}

public class AudioManager : MonoBehaviour
{
    // インスペクターでサウンドを紐付けるための内部クラス（既存）
    [System.Serializable]
    public class Sound
    {
        public SFXType _name;
        public AudioClip _clip;
    }

    // ★★★【追加】★★★ BGM用のサウンド定義クラス
    [System.Serializable]
    public class BGMSound
    {
        public BGMType _name;
        public AudioClip _clip;
    }

    [Header("音量設定")]
    [Range(0f, 1f)]
    [SerializeField]
    private float _bgmVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField]
    private float _sfxVolume = 1.0f;

    [Header("再生コンポーネント")]
    [SerializeField]
    private AudioSource _bgmSource; // ★★★【追加】★★★ BGM専用のAudioSource

    [SerializeField]
    private AudioSource _sfxSource;

    [Header("サウンドライブラリ")]
    [SerializeField]
    private BGMSound[] _bgmLibrary; // ★★★【追加】★★★ BGMのリスト

    [SerializeField]
    private Sound[] _sfxLibrary;

    // 高速アクセスのための辞書
    private Dictionary<BGMType, AudioClip> _bgmDict;
    private Dictionary<SFXType, AudioClip> _sfxDict;

    // 現在のフェード処理を保持
    private Tween _bgmFadeTween;

    void Awake()
    {
        // BGMライブラリを初期化
        _bgmDict = new Dictionary<BGMType, AudioClip>();
        foreach (var sound in _bgmLibrary)
        {
            _bgmDict[sound._name] = sound._clip;
        }

        // SFXライブラリを初期化
        _sfxDict = new Dictionary<SFXType, AudioClip>();
        foreach (var sound in _sfxLibrary)
        {
            _sfxDict[sound._name] = sound._clip;
        }
    }

    /// <summary>
    /// 指定されたBGMをクロスフェードで再生します
    /// </summary>
    /// <param name="bgmType">再生したいBGMの種類</param>
    /// <param name="fadeDuration">フェードにかける時間</param>
    public void PlayBGM(BGMType bgmType, float fadeDuration = 1.0f)
    {
        // 既存のフェード処理があれば中断
        if (_bgmFadeTween != null && _bgmFadeTween.IsActive())
        {
            _bgmFadeTween.Kill();
        }

        if (bgmType == BGMType.None || !_bgmDict.ContainsKey(bgmType))
        {
            // BGMを停止する場合
            _bgmFadeTween = _bgmSource.DOFade(0, fadeDuration).OnComplete(() => _bgmSource.Stop());
            return;
        }

        AudioClip clipToPlay = _bgmDict[bgmType];

        // 既に同じ曲が再生中なら何もしない
        if (_bgmSource.isPlaying && _bgmSource.clip == clipToPlay)
        {
            return;
        }

        // DOTweenでクロスフェード処理
        _bgmFadeTween = _bgmSource
            .DOFade(0, fadeDuration / 2)
            .OnComplete(() =>
            {
                _bgmSource.Stop();
                _bgmSource.clip = clipToPlay;
                _bgmSource.Play();
                _bgmSource.DOFade(_bgmVolume, fadeDuration / 2);
            });
    }

    /// <summary>
    /// 指定された効果音を再生します（既存）
    /// </summary>
    public void PlaySFX(SFXType sfxType)
    {
        if (_sfxDict.TryGetValue(sfxType, out AudioClip clip))
        {
            // sfxVolumeを適用
            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }
        else
        {
            Debug.LogWarning($"効果音 {sfxType} がライブラリに登録されていません。");
        }
    }
}
