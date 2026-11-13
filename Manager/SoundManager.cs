using UnityEngine;

/// <summary>
/// SE와 BGM을 재생한다. 클립은 ResourceManager로 불러오고, 재생이 끝나면 참조를 내린다.
/// SE는 동시에 20개까지고, 넘치면 가장 먼저 재생을 시작한 소리를 끊는다.
/// </summary>
public class SoundManager : ManagerBase
{
    public const int MaxSeCount = 20;

    private readonly SeVoice[] _seVoices = new SeVoice[MaxSeCount];
    private ResourceManager _resources;
    private AudioSource _bgmSource;
    private string _bgmAddress;
    private bool _bgmOwnsRef;
    private int _bgmToken;
    private int _seToken;

    private void Awake()
    {
        var gameManager = GetComponentInParent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[SoundManager] GameManager 아래에서만 사용할 수 있습니다.", this);
            return;
        }

        _resources = gameManager.ResourceManager;
        _resources.AssetReleased += OnAssetReleased;

        _bgmSource = CreateSource("Bgm");
        _bgmSource.loop = true;
        for (var i = 0; i < _seVoices.Length; i++)
        {
            _seVoices[i] = new SeVoice
            {
                Source = CreateSource("Se" + i)
            };
        }
    }

    private void OnDestroy()
    {
        if (_resources != null)
            _resources.AssetReleased -= OnAssetReleased;

        StopBgmInternal(release: true);
        for (var i = 0; i < _seVoices.Length; i++)
        {
            if (_seVoices[i] != null)
                StopSe(_seVoices[i], release: true);
        }
    }

    private void Update()
    {
        for (var i = 0; i < _seVoices.Length; i++)
        {
            var voice = _seVoices[i];
            if (voice == null || !voice.Playing || voice.Source.isPlaying)
                continue;

            StopSe(voice, release: true);
        }
    }

    public void Play(SoundType type, string address)
    {
        if (type == SoundType.Bgm)
            PlayBgmAsync(address);
        else
            PlaySeAsync(address);
    }

    public void StopBgm()
    {
        StopBgmInternal(release: true);
    }

    private async void PlaySeAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[SoundManager] SE address가 비어 있습니다.", this);
            return;
        }

        if (_resources == null)
        {
            Debug.LogError("[SoundManager] ResourceManager가 없습니다.", this);
            return;
        }

        var voice = RentSeVoice();
        var token = voice.Token;
        var resources = _resources;
        var clip = await resources.LoadAsync<AudioClip>(address);
        if (this == null)
        {
            if (clip != null)
                resources.Release(address);
            return;
        }

        if (voice.Token != token)
        {
            if (clip != null)
                _resources.Release(address);
            return;
        }

        if (clip == null)
        {
            voice.Busy = false;
            return;
        }

        voice.Address = address;
        voice.OwnsRef = true;
        voice.Source.clip = clip;
        voice.Source.loop = false;
        voice.Source.Play();
        voice.Playing = true;
    }

    private async void PlayBgmAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("[SoundManager] BGM address가 비어 있습니다.", this);
            return;
        }

        if (_resources == null)
        {
            Debug.LogError("[SoundManager] ResourceManager가 없습니다.", this);
            return;
        }

        if (_bgmAddress == address && _bgmSource != null && _bgmSource.isPlaying)
            return;

        StopBgmInternal(release: true);
        var token = _bgmToken;
        var resources = _resources;
        var clip = await resources.LoadAsync<AudioClip>(address);
        if (this == null)
        {
            if (clip != null)
                resources.Release(address);
            return;
        }

        if (token != _bgmToken)
        {
            if (clip != null)
                _resources.Release(address);
            return;
        }

        if (clip == null)
            return;

        _bgmAddress = address;
        _bgmOwnsRef = true;
        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    private SeVoice RentSeVoice()
    {
        SeVoice free = null;
        SeVoice oldest = null;
        for (var i = 0; i < _seVoices.Length; i++)
        {
            var voice = _seVoices[i];
            if (!voice.Busy)
            {
                free = voice;
                break;
            }

            if (oldest == null || voice.Token < oldest.Token)
                oldest = voice;
        }

        var picked = free ?? oldest;
        if (picked.Busy)
            StopSe(picked, release: true);

        picked.Token = ++_seToken;
        picked.Busy = true;
        picked.Playing = false;
        return picked;
    }

    private void StopSe(SeVoice voice, bool release)
    {
        var address = voice.Address;
        var ownsRef = voice.OwnsRef;
        voice.Source.Stop();
        voice.Source.clip = null;
        voice.Busy = false;
        voice.Playing = false;
        voice.OwnsRef = false;
        voice.Address = null;
        if (release && ownsRef && _resources != null)
            _resources.Release(address);
    }

    private void StopBgmInternal(bool release)
    {
        _bgmToken++;
        if (_bgmSource != null)
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        var address = _bgmAddress;
        var ownsRef = _bgmOwnsRef;
        _bgmOwnsRef = false;
        _bgmAddress = null;
        if (release && ownsRef && _resources != null)
            _resources.Release(address);
    }

    private void OnAssetReleased(string address)
    {
        if (string.IsNullOrEmpty(address))
            return;

        if (_bgmAddress == address)
            StopBgmInternal(release: false);

        for (var i = 0; i < _seVoices.Length; i++)
        {
            var voice = _seVoices[i];
            if (voice != null && voice.Address == address)
                StopSe(voice, release: false);
        }
    }

    private AudioSource CreateSource(string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private sealed class SeVoice
    {
        public AudioSource Source;
        public string Address;
        public int Token;
        public bool Busy;
        public bool Playing;
        public bool OwnsRef;
    }
}

public enum SoundType
{
    Se,
    Bgm
}
