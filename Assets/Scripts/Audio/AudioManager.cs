// ============================================================
// AudioManager.cs
// Location: Assets/Scripts/Audio/AudioManager.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Singleton audio router. Loads 5 SFX clips from Addressables.
// If a clip is missing (no audio assets imported yet), it logs
// a warning and continues — no crash, no spam.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float uiVolume = 1f;

    private AudioSource _sfxSource;
    private AudioSource _uiSource;

    private AudioClip _collectClip;
    private AudioClip _scrollUnfurlClip;
    private AudioClip _completionFanfareClip;
    private AudioClip _badgeEarnedClip;
    private AudioClip _uiTapClip;

    // Addressable keys matching the Implementation Plan audio group
    private const string KeyCollect = "audio/collect";
    private const string KeyScrollUnfurl = "audio/scroll_unfurl";
    private const string KeyCompletionFanfare = "audio/completion_fanfare";
    private const string KeyBadgeEarned = "audio/badge_earned";
    private const string KeyUITap = "audio/ui_tap";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _uiSource = gameObject.AddComponent<AudioSource>();

        StartCoroutine(LoadAllClips());
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _sfxSource.Pause();
            _uiSource.Pause();
        }
        else
        {
            _sfxSource.UnPause();
            _uiSource.UnPause();
        }
    }

    // ── Public SFX calls (called by other scripts) ──────────────

    public void PlayCollectSFX() => PlaySFX(_collectClip);
    public void PlayScrollUnfurlSFX() => PlaySFX(_scrollUnfurlClip);
    public void PlayCompletionFanfareSFX() => PlaySFX(_completionFanfareClip);
    public void PlayBadgeEarnedSFX() => PlaySFX(_badgeEarnedClip);
    public void PlayUITapSFX() => PlayUI(_uiTapClip);

    // ── Internal helpers ─────────────────────────────────────────

    void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.volume = masterVolume * sfxVolume;
        _sfxSource.PlayOneShot(clip);
    }

    void PlayUI(AudioClip clip)
    {
        if (clip == null) return;
        _uiSource.volume = masterVolume * uiVolume;
        _uiSource.PlayOneShot(clip);
    }

    IEnumerator LoadAllClips()
    {
        yield return LoadClip(KeyCollect, c => _collectClip = c);
        yield return LoadClip(KeyScrollUnfurl, c => _scrollUnfurlClip = c);
        yield return LoadClip(KeyCompletionFanfare, c => _completionFanfareClip = c);
        yield return LoadClip(KeyBadgeEarned, c => _badgeEarnedClip = c);
        yield return LoadClip(KeyUITap, c => _uiTapClip = c);
    }

    IEnumerator LoadClip(string key, System.Action<AudioClip> onLoaded)
    {
        // Check whether the key exists BEFORE calling LoadAssetAsync.
        // LoadResourceLocationsAsync never throws InvalidKeyException —
        // it simply returns an empty list when the key is missing.
        // This prevents Addressables from internally logging an exception
        // for audio clips that haven't been imported yet.
        var locationOp = Addressables.LoadResourceLocationsAsync(key, typeof(AudioClip));
        yield return locationOp;

        bool exists = locationOp.Status == AsyncOperationStatus.Succeeded
                   && locationOp.Result != null
                   && locationOp.Result.Count > 0;

        Addressables.Release(locationOp);

        if (!exists)
        {
            Debug.LogWarning($"[AudioManager] Skipping missing audio: '{key}' " +
                             "(import audio clips and add to Addressables Audio group)");
            yield break;
        }

        var handle = Addressables.LoadAssetAsync<AudioClip>(key);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            onLoaded(handle.Result);
            Debug.Log($"[AudioManager] Loaded: {key}");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Failed to load audio: {key}");
        }
    }
}
