using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum AudioPriority
{
    VeryLow = 0,
    Low = 25,
    Normal = 50,
    High = 75,
    Critical = 100
}
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup voiceGroup;
    [SerializeField] private AudioMixerGroup ambientGroup;

    [Header("Voice Pool")]
    [SerializeField, Min(1)] private int voicePoolSize = 24;

    [Header("Mixer Parameters")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string sfxVolumeParameter = "SFXVolume";
    [SerializeField] private string voiceVolumeParameter = "VoiceVolume";
    [SerializeField] private string ambientVolumeParameter = "AmbientVolume";

    private readonly List<AudioVoice> voices = new List<AudioVoice>();

    private Transform voiceContainer;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateContainers();
        CreateMusicSource();
        CreateVoicePool();
    }

    private void Update()
    {
        CleanupFinishedVoices();
    }

    // =========================================================
    // Public API
    // =========================================================

    public void Play(SoundDef sound)
    {
        Play(sound, Vector3.zero, false);
    }

    public void Play(SoundDef sound, Vector3 position)
    {
        Play(sound, position, true);
    }

    public void PlayMusic(SoundDef sound)
    {
        if (sound == null)
            return;

        AudioClip clip = sound.GetRandomClip();

        if (clip == null)
            return;

        musicSource.Stop();

        musicSource.clip = clip;
        musicSource.volume = sound.GetRandomVolume();
        musicSource.pitch = sound.GetRandomPitch();
        musicSource.loop = true;
        musicSource.outputAudioMixerGroup = musicGroup;

        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    // =========================================================
    // Main Playback
    // =========================================================

    private void Play(
        SoundDef sound,
        Vector3 position,
        bool hasPosition)
    {
        if (sound == null)
            return;

        AudioClip clip = sound.GetRandomClip();

        if (clip == null)
            return;

        if (!CanPlaySound(sound))
            return;

        AudioVoice voice = GetVoice(sound.Priority);

        if (voice == null)
            return;

        ConfigureVoice(
            voice,
            sound,
            clip,
            position,
            hasPosition);

        voice.Play(sound);
    }

    // =========================================================
    // Voice Limiting
    // =========================================================

    private bool CanPlaySound(SoundDef sound)
    {
        int activeCount = 0;

        for (int i = 0; i < voices.Count; i++)
        {
            AudioVoice voice = voices[i];

            if (!voice.IsPlaying)
                continue;

            if (voice.Sound == sound)
                activeCount++;
        }

        return activeCount < sound.MaxSimultaneous;
    }

    // =========================================================
    // Voice Selection
    // =========================================================

    private AudioVoice GetVoice(AudioPriority requestedPriority)
    {
        // First, look for an unused voice.
        for (int i = 0; i < voices.Count; i++)
        {
            if (!voices[i].IsPlaying)
                return voices[i];
        }

        // Pool is full.
        // Find the lowest-priority active voice.
        AudioVoice lowestPriorityVoice = null;

        for (int i = 0; i < voices.Count; i++)
        {
            AudioVoice voice = voices[i];

            if (!voice.IsPlaying)
                continue;

            if (lowestPriorityVoice == null ||
                voice.Priority < lowestPriorityVoice.Priority)
            {
                lowestPriorityVoice = voice;
            }
        }

        if (lowestPriorityVoice == null)
            return null;

        // Don't interrupt a sound with equal or higher priority.
        if (requestedPriority <= lowestPriorityVoice.Priority)
            return null;

        lowestPriorityVoice.Stop();

        return lowestPriorityVoice;
    }

    // =========================================================
    // Voice Configuration
    // =========================================================

    private void ConfigureVoice(
        AudioVoice voice,
        SoundDef sound,
        AudioClip clip,
        Vector3 position,
        bool hasPosition)
    {
        AudioSource source = voice.Source;

        source.transform.position = position;

        source.clip = clip;
        source.volume = sound.GetRandomVolume();
        source.pitch = sound.GetRandomPitch();

        source.spatialBlend = hasPosition
            ? sound.SpatialBlend
            : 0f;

        source.minDistance = sound.MinDistance;
        source.maxDistance = sound.MaxDistance;

        source.loop = false;
        source.outputAudioMixerGroup =
            GetMixerGroup(sound.Category);
    }

    // =========================================================
    // Cleanup
    // =========================================================

    private void CleanupFinishedVoices()
    {
        for (int i = 0; i < voices.Count; i++)
        {
            AudioVoice voice = voices[i];

            if (!voice.IsPlaying)
                voice.Clear();
        }
    }

    // =========================================================
    // Pool Creation
    // =========================================================

    private void CreateVoicePool()
    {
        for (int i = 0; i < voicePoolSize; i++)
        {
            CreateVoice(i);
        }
    }

    private void CreateVoice(int index)
    {
        GameObject sourceObject =
            new GameObject($"SFX Voice {index}");

        sourceObject.transform.SetParent(voiceContainer);

        AudioSource source =
            sourceObject.AddComponent<AudioSource>();

        source.playOnAwake = false;

        AudioVoice voice = new AudioVoice(source);

        voices.Add(voice);
    }

    private void CreateContainers()
    {
        GameObject container =
            new GameObject("Audio Voices");

        container.transform.SetParent(transform);

        voiceContainer = container.transform;
    }

    private void CreateMusicSource()
    {
        GameObject musicObject =
            new GameObject("Music Source");

        musicObject.transform.SetParent(transform);

        musicSource =
            musicObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.outputAudioMixerGroup = musicGroup;
    }

    // =========================================================
    // Mixer
    // =========================================================

    public void SetMasterVolume(float volume)
    {
        SetMixerVolume(
            masterGroup,
            masterVolumeParameter,
            volume);
    }

    public void SetMusicVolume(float volume)
    {
        SetMixerVolume(
            musicGroup,
            musicVolumeParameter,
            volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetMixerVolume(
            sfxGroup,
            sfxVolumeParameter,
            volume);
    }

    public void SetVoiceVolume(float volume)
    {
        SetMixerVolume(
            voiceGroup,
            voiceVolumeParameter,
            volume);
    }

    public void SetAmbientVolume(float volume)
    {
        SetMixerVolume(
            ambientGroup,
            ambientVolumeParameter,
            volume);
    }

    private void SetMixerVolume(
        AudioMixerGroup group,
        string parameter,
        float volume)
    {
        if (group == null)
            return;

        volume = Mathf.Clamp01(volume);

        float decibels;

        if (volume <= 0.0001f)
            decibels = -80f;
        else
            decibels = Mathf.Log10(volume) * 20f;

        group.audioMixer.SetFloat(
            parameter,
            decibels);
    }

    // =========================================================
    // Mixer Routing
    // =========================================================

    private AudioMixerGroup GetMixerGroup(
        AudioCategory category)
    {
        switch (category)
        {
            case AudioCategory.Music:
                return musicGroup;

            case AudioCategory.Voice:
                return voiceGroup;

            case AudioCategory.Ambient:
                return ambientGroup;

            case AudioCategory.SFX:
            default:
                return sfxGroup;
        }
    }

    // =========================================================
    // Voice
    // =========================================================

    private class AudioVoice
    {
        public readonly AudioSource Source;

        public SoundDef Sound { get; private set; }
        public AudioPriority Priority { get; private set; }

        public bool IsPlaying =>
            Source != null && Source.isPlaying;

        public AudioVoice(AudioSource source)
        {
            Source = source;
        }

        public void Play(SoundDef sound)
        {
            Sound = sound;
            Priority = sound.Priority;

            Source.Play();
        }

        public void Stop()
        {
            Source.Stop();
            Clear();
        }

        public void Clear()
        {
            Sound = null;
            Priority = AudioPriority.VeryLow;

            Source.clip = null;
            Source.loop = false;
        }
    }
}