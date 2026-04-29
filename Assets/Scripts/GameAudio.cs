using UnityEngine;

/// <summary>
/// SFX hub: procedural clips by default, or optional authored clips under <c>Resources/Audio/</c>.
/// Uses a 2D <see cref="AudioSource"/> so UI/canvas world positions do not get distance-muted like <see cref="AudioSource.PlayClipAtPoint"/>.
/// Respects <see cref="AudioSettings"/> master / SFX via per-shot volume scale.
/// </summary>
public static class GameAudio
{
    const int SampleRate = 44100;

    static AudioClip _fruitSlice;
    static AudioClip _bombHit;
    static AudioClip _gameOverSting;
    static AudioClip _uiTick;

    static AudioSource _sfx;

    static float SfxGain => Mathf.Clamp01(AudioSettings.SfxVolume01) * Mathf.Clamp01(AudioSettings.MasterVolume01);

    /// <param name="_">World position kept for API symmetry / future stereo pan.</param>
    public static void PlayFruitSlice(Vector3 _)
    {
        EnsureFruitSlice();
        PlayOneShot(_fruitSlice, 0.92f);
    }

    /// <param name="_">World position kept for API symmetry / future stereo pan.</param>
    public static void PlayBombHit(Vector3 _)
    {
        EnsureBombHit();
        PlayOneShot(_bombHit, 1f);
    }

    public static void PlayGameOverSting()
    {
        EnsureGameOver();
        PlayOneShot(_gameOverSting, 0.85f);
    }

    public static void PlayUiTick()
    {
        EnsureUiTick();
        PlayOneShot(_uiTick, 0.72f);
    }

    static void EnsureSfxSource()
    {
        if (_sfx != null)
            return;

        var go = new GameObject("FN_GameAudio_Sfx");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _sfx = go.AddComponent<AudioSource>();
        _sfx.spatialBlend = 0f;
        _sfx.playOnAwake = false;
        _sfx.loop = false;
        _sfx.dopplerLevel = 0f;
        _sfx.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    static void PlayOneShot(AudioClip clip, float relativeVolume)
    {
        if (clip == null || SfxGain <= 0.001f)
            return;
        EnsureSfxSource();
        float scale = SfxGain * Mathf.Clamp01(relativeVolume);
        _sfx.PlayOneShot(clip, scale);
    }

    static AudioClip TryResources(string path)
    {
        return Resources.Load<AudioClip>(path);
    }

    static void EnsureFruitSlice()
    {
        if (_fruitSlice != null)
            return;

        _fruitSlice = TryResources("Audio/FruitSlice");
        if (_fruitSlice != null)
            return;

        float dur = 0.11f;
        int n = Mathf.CeilToInt(SampleRate * dur);
        var data = new float[n];
        float seed = Random.Range(1f, 99f);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float env = Mathf.Exp(-t * 38f);
            float swoosh = Mathf.Sin((seed + t * 3800f) * Mathf.PI * 2f) * 0.35f;
            float noise = (Random.value * 2f - 1f) * 0.55f;
            float ping = Mathf.Sin(t * Mathf.PI * 2f * 920f) * Mathf.Exp(-t * 55f) * 0.65f;
            data[i] = Mathf.Clamp((noise * swoosh + ping) * env, -1f, 1f) * 0.85f;
        }

        _fruitSlice = AudioClip.Create("FN_FruitSlice", n, 1, SampleRate, false);
        _fruitSlice.SetData(data, 0);
    }

    static void EnsureBombHit()
    {
        if (_bombHit != null)
            return;

        _bombHit = TryResources("Audio/BombHit");
        if (_bombHit != null)
            return;

        float dur = 0.38f;
        int n = Mathf.CeilToInt(SampleRate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float env = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(t / dur));
            float drop = Mathf.Sin(t * Mathf.PI * 2f * Mathf.Lerp(220f, 45f, Mathf.Clamp01(t * 3f))) * 0.55f;
            float rumble = (Random.value * 2f - 1f) * 0.55f * Mathf.Exp(-t * 6f);
            data[i] = Mathf.Clamp((drop + rumble) * env, -1f, 1f) * 0.95f;
        }

        _bombHit = AudioClip.Create("FN_BombHit", n, 1, SampleRate, false);
        _bombHit.SetData(data, 0);
    }

    static void EnsureGameOver()
    {
        if (_gameOverSting != null)
            return;

        _gameOverSting = TryResources("Audio/GameOver");
        if (_gameOverSting != null)
            return;

        float dur = 0.95f;
        int n = Mathf.CeilToInt(SampleRate * dur);
        var data = new float[n];

        void Tone(float startT, float lengthT, float freq, float amp)
        {
            int i0 = Mathf.RoundToInt(startT * SampleRate);
            int i1 = Mathf.Min(n - 1, Mathf.RoundToInt((startT + lengthT) * SampleRate));
            for (int i = i0; i <= i1; i++)
            {
                float lt = (i - i0) / (float)(i1 - i0 + 1);
                float env = Mathf.Sin(lt * Mathf.PI);
                float ph = i / (float)SampleRate * Mathf.PI * 2f * freq;
                data[i] += Mathf.Sin(ph) * amp * env * 0.55f;
            }
        }

        Tone(0f, 0.22f, 392f, 1f);
        Tone(0.2f, 0.24f, 349f, 0.95f);
        Tone(0.42f, 0.48f, 293f, 0.9f);

        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(data[i], -1f, 1f);

        _gameOverSting = AudioClip.Create("FN_GameOver", n, 1, SampleRate, false);
        _gameOverSting.SetData(data, 0);
    }

    static void EnsureUiTick()
    {
        if (_uiTick != null)
            return;

        _uiTick = TryResources("Audio/UiTick");
        if (_uiTick != null)
            return;

        float dur = 0.055f;
        int n = Mathf.CeilToInt(SampleRate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float env = Mathf.Exp(-t * 95f);
            data[i] = Mathf.Sin(t * Mathf.PI * 2f * 1180f) * env * 0.72f;
        }

        _uiTick = AudioClip.Create("FN_UiTick", n, 1, SampleRate, false);
        _uiTick.SetData(data, 0);
    }
}
