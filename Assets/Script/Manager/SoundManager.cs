using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SoundManager : MonoBehaviour
{
    private const string MasterVolumeKey = "Audio.MasterVolume";
    private const string BgmVolumeKey = "Audio.BgmVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private readonly HashSet<Button> hookedButtons = new HashSet<Button>();
    private AudioClip buttonClickSfx;
    private float nextButtonScanTime;

    public static SoundManager Instance { get; private set; }
    public float MasterVolume { get; private set; } = 1f;
    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    public event Action<float> SfxVolumeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateManager()
    {
        if (Instance == null)
        {
            new GameObject("SoundManager").AddComponent<SoundManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

        bgmSource = CreateSource("BGM", true);
        sfxSource = CreateSource("SFX", false);
        buttonClickSfx = Resources.Load<AudioClip>("Audio/SFX/ButtonClick");
        AudioListener.volume = MasterVolume;
        bgmSource.volume = BgmVolume;
        sfxSource.volume = SfxVolume;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextButtonScanTime)
            return;

        nextButtonScanTime = Time.unscaledTime + 0.15f;
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button == null || !hookedButtons.Add(button))
                continue;

            bool hasDedicatedSound = HasDedicatedConfirmSound(button);
            button.onClick.AddListener(() =>
            {
                if (!hasDedicatedSound)
                    PlaySfx(buttonClickSfx);
            });
        }

        hookedButtons.RemoveWhere(button => button == null);
    }

    private bool HasDedicatedConfirmSound(Button button)
    {
        if (button == null)
            return false;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == "ConfirmCharacter")
                return true;
        }

        return false;
    }

    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
    }

    public void SetBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = BgmVolume;
        }

        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = SfxVolume;
        }

        SfxVolumeChanged?.Invoke(SfxVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
    }

    public void PlayBgm(AudioClip clip, bool restartIfPlaying = false)
    {
        if (clip == null || bgmSource == null)
        {
            return;
        }

        if (!restartIfPlaying && bgmSource.isPlaying && bgmSource.clip == clip)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        var sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        var source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            PlayerPrefs.Save();
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
