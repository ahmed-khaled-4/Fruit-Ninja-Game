using UnityEngine;

/// <summary>
/// Persists volume prefs. Drives <see cref="AudioListener.volume"/> via master level.
/// Music/SFX sliders are stored separately for UI and future per-source tuning.
/// </summary>
public static class AudioSettings
{
    const string MasterKey = "FN_MasterVol";
    const string MusicKey = "FN_MusicVol";
    const string SfxKey = "FN_SfxVol";

    public static float MasterVolume01
    {
        get => PlayerPrefs.GetFloat(MasterKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyToListener();
        }
    }

    public static float MusicVolume01
    {
        get => PlayerPrefs.GetFloat(MusicKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyToListener();
        }
    }

    public static float SfxVolume01
    {
        get => PlayerPrefs.GetFloat(SfxKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyToListener();
        }
    }

    /// <summary>Combined perceived volume: master scaled by average of music/sfx knobs.</summary>
    public static void ApplyToListener()
    {
        float blend = (MusicVolume01 + SfxVolume01) * 0.5f;
        AudioListener.volume = Mathf.Clamp01(MasterVolume01 * Mathf.Clamp01(blend));
    }

    public static void LoadFromPrefs()
    {
        ApplyToListener();
    }
}
