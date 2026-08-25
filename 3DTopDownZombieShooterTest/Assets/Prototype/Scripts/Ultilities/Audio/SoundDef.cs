using UnityEngine;

public enum AudioCategory
{
    SFX,
    Voice,
    Ambient,
    Music
}

[CreateAssetMenu(menuName = "Audio/Sound Data")]
public class SoundDef : ScriptableObject
{
    [Header("General")]
    [SerializeField] private AudioCategory category;
    [SerializeField] private AudioPriority priority = AudioPriority.Normal;
    [SerializeField] private AudioClip[] clips;

    [Header("Voice Limiting")]
    [SerializeField, Min(1)] private int maxSimultaneous = 4;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0f, 1f)] private float volumeVariation;

    [Header("Pitch")]
    [SerializeField] private float pitch = 1f;
    [SerializeField, Range(0f, 1f)] private float pitchVariation;

    [Header("3D")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 30f;

    public AudioCategory Category => category;
    public AudioPriority Priority => priority;
    public int MaxSimultaneous => maxSimultaneous;

    public float Volume => volume;
    public float VolumeVariation => volumeVariation;

    public float Pitch => pitch;
    public float PitchVariation => pitchVariation;

    public float SpatialBlend => spatialBlend;
    public float MinDistance => minDistance;
    public float MaxDistance => maxDistance;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }

    public float GetRandomVolume()
    {
        return Mathf.Clamp01(
            volume + Random.Range(-volumeVariation, volumeVariation));
    }

    public float GetRandomPitch()
    {
        return Mathf.Max(
            0.01f,
            pitch + Random.Range(-pitchVariation, pitchVariation));
    }
}