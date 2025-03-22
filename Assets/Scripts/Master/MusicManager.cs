using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public GameObject sfxPrefab;

    [Header("Audio Clips")]
    public AudioClip defaultMusic;

    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
    {
        // Create instance
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }

        // Load volume settings
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        if (musicSource != null) {
            musicSource.volume = musicVolume;
            if (defaultMusic != null) {
                PlayMusic(defaultMusic);
            }
        }
    }

    // Play background music with fade
    public void PlayMusic(AudioClip newMusic, float fadeDuration = 2f)
    {
		// Check if the song is the same
        if (musicSource.clip == newMusic) return;

        StartCoroutine(FadeMusic(newMusic, fadeDuration));
    }

    // Stop background music
    public void StopMusic() {
        musicSource.Stop();
    }

    // Adjust Music Volume
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    // Smoothly fade music
    private IEnumerator FadeMusic(AudioClip newMusic, float duration)
    {
        float startVolume = musicSource.volume;

        // Fade out
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newMusic;
        musicSource.Play();

        // Fade in
        while (musicSource.volume < musicVolume)
        {
            musicSource.volume += musicVolume * Time.deltaTime / duration;
            yield return null;
        }
    }

    // Play sound effect
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

		GameObject sfxObject = Instantiate(sfxPrefab);
        AudioSource sfxSource = sfxObject.GetComponent<AudioSource>();
        sfxSource.clip = clip;
        sfxSource.volume = volume * sfxVolume;
        sfxSource.Play();

		Destroy(sfxObject, clip.length);
    }

    // Adjust SFX Volume
    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }
}
