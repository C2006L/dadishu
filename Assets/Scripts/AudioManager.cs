using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip hitSound;

    private const string MasterKey = "WhackAMole.MasterVolume";
    private const string MusicKey = "WhackAMole.MusicEnabled";
    private const string SfxKey = "WhackAMole.SfxEnabled";

    public float MasterVolume { get; private set; } = 0.8f;
    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;

    private void Awake()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterKey, 0.8f);
        MusicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        SfxEnabled = PlayerPrefs.GetInt(SfxKey, 1) == 1;
        ApplySettings();
    }

    public void PlayBackgroundMusic()
    {
        if (musicSource == null || backgroundMusic == null || musicSource.isPlaying) return;
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.volume = 0.36f;
        musicSource.Play();
    }

    public void PlayHitSound()
    {
        PlayMoleSound(MoleType.Normal);
    }

    public void PlayMoleSound(MoleType type)
    {
        if (!SfxEnabled || effectsSource == null || hitSound == null) return;
        effectsSource.pitch = type == MoleType.Reward ? 1.28f : type == MoleType.Bomb ? 0.62f : 1f;
        effectsSource.PlayOneShot(hitSound, type == MoleType.Bomb ? 1f : 0.78f);
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterKey, MasterVolume);
        ApplySettings();
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        PlayerPrefs.SetInt(MusicKey, enabled ? 1 : 0);
        ApplySettings();
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;
        PlayerPrefs.SetInt(SfxKey, enabled ? 1 : 0);
        ApplySettings();
    }

    public void ToggleSfx() => SetSfxEnabled(!SfxEnabled);

    private void ApplySettings()
    {
        AudioListener.volume = MasterVolume;
        if (musicSource != null) musicSource.mute = !MusicEnabled;
        if (effectsSource != null) effectsSource.mute = !SfxEnabled;
        PlayerPrefs.Save();
    }
}
