using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public sealed class SoundManager : MonoBehaviour
{
    private const string MasterVolumeKey = "Audio.MasterVolume";
    private const string BgmVolumeKey = "Audio.BgmVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";
    private const float SettingsBgmVolumeScale = 0.2f;

    private float bgmVolumeScale = 1f;
    private int settingsOpenCount;
    private bool upgradeSelectionOpen;
    private readonly List<AudioSource> stoppedLoops = new List<AudioSource>();
    private AudioSource bgmSource;
    private readonly List<OneShotVoice> oneShotVoices = new List<OneShotVoice>();
    private readonly Stack<AudioSource> availableOneShotSources = new Stack<AudioSource>();
    private readonly Dictionary<AudioSource, int> loopSoundIds = new Dictionary<AudioSource, int>();
    private readonly Dictionary<AudioSource, int> loopStartedFrames = new Dictionary<AudioSource, int>();
    private readonly Dictionary<AudioSource, EnemySoundLifetime> enemyLoopLifetimes = new();
    private readonly List<AudioSource> fadedLoops = new();
    private readonly HashSet<Button> hookedButtons = new HashSet<Button>();
    private readonly HashSet<Toggle> hookedToggles = new HashSet<Toggle>();
    private int lastUiClickFrame = -1;
    private int lastUiHoverFrame = -1;
    [SerializeField, LabelText("사운드 목록")]
    [ListDrawerSettings(ListElementLabelName = "name", DefaultExpandedState = false, ShowIndexLabels = false)]
    private List<SoundEntry> sounds = new List<SoundEntry>();
    private readonly Dictionary<AudioSource, float> loopVolumes = new Dictionary<AudioSource, float>();
    public IReadOnlyList<SoundEntry> Sounds => sounds;
    private float nextButtonScanTime;

    private struct OneShotVoice
    {
        public AudioSource Source;
        public int SoundId;
        public float VolumeScale;
        public int StartedFrame;
        public EnemySoundLifetime Lifetime;
    }

    private sealed class EnemySoundLifetime
    {
        public EnemyCombat Enemy;
        public PooledObject Pool;
        public uint SpawnVersion;
        public float Duration;
        public float Elapsed = -1f;
        public float Gain = 1f;

        public bool Advance(float delta)
        {
            if (Elapsed < 0f)
            {
                if (Enemy != null && Enemy.gameObject.activeInHierarchy &&
                    !Enemy.isDead && Enemy.hp > 0f &&
                    (Pool == null || Pool.SpawnVersion == SpawnVersion))
                    return false;
                Elapsed = 0f;
            }
            Elapsed += delta;
            Gain = Mathf.Clamp01(1f - Elapsed / Duration);
            return Gain <= 0f;
        }
    }

    public static SoundManager Instance { get; private set; }
    public float MasterVolume { get; private set; } = 1f;
    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    public event Action<float> SfxVolumeChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

        Transform authoredBgm = transform.Find("BGM");
        bgmSource = authoredBgm != null ? authoredBgm.GetComponent<AudioSource>() : null;
        if (bgmSource == null)
            bgmSource = CreateSource("BGM", true);
        AudioListener.volume = MasterVolume;
        AudioListener.pause = Time.timeScale <= 0f;
        ApplyBgmVolume();
    }

    private void Update()
    {
        bool upgradeOpen = LevelManager.Instance != null &&
            LevelManager.Instance.upgradeUI != null &&
            LevelManager.Instance.upgradeUI.activeInHierarchy;
        if (upgradeSelectionOpen != upgradeOpen)
        {
            upgradeSelectionOpen = upgradeOpen;
            ApplyBgmVolume();
        }

        // Time.timeScale pauses gameplay timing, but does not pause audio by itself.
        // Pause every AudioSource globally while gameplay is paused, then resume them.
        AudioListener.pause = Time.timeScale <= 0f;

        UpdateEnemySoundFades();
        CleanupStoppedLoops();
        CleanupFinishedOneShots();
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
                    PlayUiClick();
            });
        }

        foreach (Toggle toggle in FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (toggle == null || !hookedToggles.Add(toggle))
                continue;
            toggle.onValueChanged.AddListener(selected =>
            {
                if (selected && toggle.IsActive() && toggle.IsInteractable())
                {
                    if (toggle.GetComponentInParent<TMPro.TMP_Dropdown>() != null)
                        PlayUiConfirm();
                    else
                        PlayUiClick();
                }
            });
        }
        hookedToggles.RemoveWhere(toggle => toggle == null);
        hookedButtons.RemoveWhere(button => button == null);
    }

    public void PlayUiClick()
    {
        // A dropdown row can also be a Toggle; emit only one click per action.
        if (lastUiClickFrame == Time.frameCount)
            return;
        lastUiClickFrame = Time.frameCount;
        PlayOneShotIgnoringPause(SoundIds.ButtonClick);
    }

    public void PlayUiConfirm()
    {
        // The dropdown callback and its option Toggle can fire in the same frame.
        if (lastUiClickFrame == Time.frameCount)
            return;
        lastUiClickFrame = Time.frameCount;
        PlayOneShotIgnoringPause(SoundIds.CharacterConfirm);
    }

    public void PlayUiHover()
    {
        if (lastUiHoverFrame == Time.frameCount)
            return;
        lastUiHoverFrame = Time.frameCount;
        PlayOneShotIgnoringPause(SoundIds.UiHover);
    }

    private bool HasDedicatedConfirmSound(Button button)
    {
        if (button == null)
            return false;

        foreach (StagePlanetMenu menu in FindObjectsByType<StagePlanetMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (menu.IsStartButton(button))
                return true;

        foreach (CharacterProgressionPanel panel in FindObjectsByType<CharacterProgressionPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (panel.IsUpgradeButton(button))
                return true;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            string methodName = button.onClick.GetPersistentMethodName(i);
            if (methodName == "ConfirmCharacter" ||
                methodName == "Upgrade" ||
                methodName == "UpgradeIsSelected" ||
                methodName == "PlaySelectedStage")
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
        ApplyBgmVolume();

        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
    }

    public void BeginSettingsAudio()
    {
        settingsOpenCount++;
        ApplyBgmVolume();
    }

    public void EndSettingsAudio()
    {
        settingsOpenCount = Mathf.Max(0, settingsOpenCount - 1);
        ApplyBgmVolume();
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource == null)
            return;

        bool selectionOrSettingsOpen = settingsOpenCount > 0 || upgradeSelectionOpen;
        bgmSource.ignoreListenerPause = selectionOrSettingsOpen;
        bgmSource.volume = BgmVolume * bgmVolumeScale *
            (selectionOrSettingsOpen ? SettingsBgmVolumeScale : 1f);
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        foreach (var pair in loopVolumes)
            if (pair.Key != null) pair.Key.volume = pair.Value * SfxVolume *
                (enemyLoopLifetimes.TryGetValue(pair.Key, out var lifetime) ? lifetime.Gain : 1f);
        foreach (OneShotVoice voice in oneShotVoices)
            if (voice.Source != null) voice.Source.volume = voice.VolumeScale * SfxVolume *
                (voice.Lifetime != null ? voice.Lifetime.Gain : 1f);
        SfxVolumeChanged?.Invoke(SfxVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
    }

    public AudioSource Play(int id, float volumeScale = 1f, bool loop = false,
        bool bgm = false, bool restartIfPlaying = false, EnemyCombat enemy = null)
    {
        SoundEntry entry = sounds.Find(sound => sound != null && sound.id == id);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"사운드 ID {id}의 파일이 등록되지 않았습니다.", this);
            return null;
        }

        volumeScale = Mathf.Clamp01(volumeScale);
        if (bgm)
        {
            if (!restartIfPlaying && bgmSource.isPlaying && bgmSource.clip == entry.clip)
                return bgmSource;
            bgmSource.Stop();
            bgmSource.clip = entry.clip;
            bgmSource.loop = loop;
            bgmVolumeScale = volumeScale;
            ApplyBgmVolume();
            bgmSource.Play();
            return bgmSource;
        }

        if (loop)
        {
            if (!CanStartVoice(entry))
                return null;
            volumeScale *= Mathf.Clamp01(entry.volume);
            AudioSource source = CreateSource(entry.name, true);
            source.clip = entry.clip;
            source.volume = SfxVolume * volumeScale;
            loopVolumes.Add(source, volumeScale);
            loopSoundIds.Add(source, id);
            loopStartedFrames.Add(source, Time.frameCount);
            var lifetime = CreateEnemySoundLifetime(entry, enemy);
            if (lifetime != null)
                enemyLoopLifetimes.Add(source, lifetime);
            source.Play();
            return source;
        }

        return PlayOneShot(entry, volumeScale, false, enemy);
    }

    public void PlayOneShotIgnoringPause(int id, float volumeScale = 1f)
    {
        SoundEntry entry = sounds.Find(sound => sound != null && sound.id == id);
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"사운드 ID {id}의 파일이 등록되지 않았습니다.", this);
            return;
        }

        PlayOneShot(entry, Mathf.Clamp01(volumeScale), true);
    }

    private AudioSource PlayOneShot(SoundEntry entry, float volumeScale, bool ignorePause, EnemyCombat enemy = null)
    {
        if (!CanStartVoice(entry))
            return null;

        // Keep the correction in the stored scale so slider changes preserve the balance.
        volumeScale *= Mathf.Clamp01(entry.volume);

        AudioSource source = null;
        while (availableOneShotSources.Count > 0 && source == null)
            source = availableOneShotSources.Pop();
        if (source == null)
            source = CreateSource("SFX", false);

        source.gameObject.SetActive(true);
        source.clip = entry.clip;
        source.loop = false;
        source.pitch = 1f;
        source.ignoreListenerPause = ignorePause;
        source.volume = SfxVolume * volumeScale;
        source.Play();
        oneShotVoices.Add(new OneShotVoice
        {
            Source = source,
            SoundId = entry.id,
            VolumeScale = volumeScale,
            StartedFrame = Time.frameCount,
            Lifetime = CreateEnemySoundLifetime(entry, enemy)
        });
        return source;
    }

    private bool CanStartVoice(SoundEntry entry)
    {
        CleanupStoppedLoops();
        CleanupFinishedOneShots();
        int count = 0;
        foreach (OneShotVoice voice in oneShotVoices)
            if (voice.SoundId == entry.id) count++;
        foreach (int soundId in loopSoundIds.Values)
            if (soundId == entry.id) count++;
        return count < Mathf.Max(1, entry.maxConcurrentVoices);
    }

    private static EnemySoundLifetime CreateEnemySoundLifetime(SoundEntry entry, EnemyCombat enemy)
    {
        if (!entry.fadeOnEnemyDeath || enemy == null)
            return null;
        var pool = enemy.GetComponent<PooledObject>();
        return new EnemySoundLifetime
        {
            Enemy = enemy,
            Pool = pool,
            SpawnVersion = pool != null ? pool.SpawnVersion : 0,
            Duration = Mathf.Max(0.01f, entry.enemyDeathFadeSeconds)
        };
    }

    private void UpdateEnemySoundFades()
    {
        for (int i = oneShotVoices.Count - 1; i >= 0; i--)
        {
            var voice = oneShotVoices[i];
            if (voice.Lifetime == null)
                continue;
            if (voice.Lifetime.Advance(Time.unscaledDeltaTime))
                ReleaseOneShot(i);
            else if (voice.Source != null)
                voice.Source.volume = voice.VolumeScale * SfxVolume * voice.Lifetime.Gain;
        }

        fadedLoops.Clear();
        foreach (var pair in enemyLoopLifetimes)
        {
            if (pair.Value.Advance(Time.unscaledDeltaTime))
                fadedLoops.Add(pair.Key);
            else if (pair.Key != null && loopVolumes.TryGetValue(pair.Key, out float scale))
                pair.Key.volume = scale * SfxVolume * pair.Value.Gain;
        }
        foreach (var source in fadedLoops)
            Stop(source);
    }

    private void ReleaseOneShot(int index)
    {
        var source = oneShotVoices[index].Source;
        oneShotVoices.RemoveAt(index);
        if (source == null)
            return;
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        availableOneShotSources.Push(source);
    }

    private void CleanupFinishedOneShots()
    {
        for (int i = oneShotVoices.Count - 1; i >= 0; i--)
        {
            OneShotVoice voice = oneShotVoices[i];
            AudioSource source = voice.Source;
            if (source != null &&
                (source.isPlaying || voice.StartedFrame == Time.frameCount ||
                 (AudioListener.pause && !source.ignoreListenerPause)))
                continue;

            ReleaseOneShot(i);
        }
    }

    public void StopBgm()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void Stop(AudioSource source)
    {
        if (source == null) return;
        if (source == bgmSource) { source.Stop(); return; }
        if (!loopVolumes.Remove(source)) return;
        loopSoundIds.Remove(source);
        loopStartedFrames.Remove(source);
        enemyLoopLifetimes.Remove(source);
        source.Stop();
        Destroy(source.gameObject);
    }

    private void CleanupStoppedLoops()
    {
        stoppedLoops.Clear();
        foreach (var pair in loopVolumes)
            if (pair.Key == null || (!pair.Key.isPlaying &&
                (!loopStartedFrames.TryGetValue(pair.Key, out int startedFrame) || startedFrame != Time.frameCount) &&
                !(AudioListener.pause && !pair.Key.ignoreListenerPause)))
                stoppedLoops.Add(pair.Key);
        foreach (AudioSource source in stoppedLoops)
        {
            loopVolumes.Remove(source);
            loopSoundIds.Remove(source);
            loopStartedFrames.Remove(source);
            enemyLoopLifetimes.Remove(source);
            if (source != null) Destroy(source.gameObject);
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
