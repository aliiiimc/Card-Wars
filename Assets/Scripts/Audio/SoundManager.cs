using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    [Header("Music")]
    public AudioClip themeClip;
    [Range(0f,1f)] public float musicVolume = 0.6f;

    [Header("SFX")]
    public AudioSource sfxSource;
    [Range(0f,1f)] public float sfxVolume = 0.9f;

    private AudioSource musicSource;

    public static SoundManager GetOrCreate()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<SoundManager>();
        if (instance != null) return instance;

        GameObject go = new GameObject("SoundManager");
        instance = go.AddComponent<SoundManager>();
        DontDestroyOnLoad(go);
        instance.EnsureSources();
        return instance;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureSources();
    }

    void EnsureSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }
        }

        if (sfxSource == null)
        {
            // create a dedicated SFX source if none assigned
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    public void PlayTheme()
    {
        if (themeClip == null)
        {
            return;
        }

        EnsureSources();
        musicSource.clip = themeClip;
        musicSource.volume = musicVolume;
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void StopTheme()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        EnsureSources();
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeScale));
    }
}
