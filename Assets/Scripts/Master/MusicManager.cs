using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public GameObject sfxPrefab;

    [Header("Audio Clips")]
    public AudioClip defaultMusic;

    [Header("Mixer")]
    public AudioMixer audioMixer; // Drag your AudioMixer here in the inspector

    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }

        // Load saved volume
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);

        if (musicSource != null && defaultMusic != null) {
            PlayMusic(defaultMusic);
        }
    }

    // Play background music with fade
    public void PlayMusic(AudioClip newMusic, float fadeDuration = 2f)
    {
        if (musicSource.clip == newMusic) return;
        StartCoroutine(FadeMusic(newMusic, fadeDuration));
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();

        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();

        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
    }

    private IEnumerator FadeMusic(AudioClip newMusic, float duration)
    {
        float startVolume;
        audioMixer.GetFloat("MusicVolume", out startVolume);
        startVolume = Mathf.Pow(10f, startVolume / 20f); // convert dB back to 0–1

        float t = 0f;
        while (t < duration)
        {
            float volume = Mathf.Lerp(startVolume, 0f, t / duration);
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
            t += Time.deltaTime;
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newMusic;
        musicSource.Play();

        t = 0f;
        while (t < duration)
        {
            float volume = Mathf.Lerp(0f, musicVolume, t / duration);
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
            t += Time.deltaTime;
            yield return null;
        }

        SetMusicVolume(musicVolume); // Ensure final value is applied
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        GameObject sfxObject = Instantiate(sfxPrefab);
        AudioSource sfxSource = sfxObject.GetComponent<AudioSource>();
        sfxSource.clip = clip;
        sfxSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
        sfxSource.volume = volume; // relative volume
        sfxSource.Play();

        Destroy(sfxObject, clip.length);
    }
}
