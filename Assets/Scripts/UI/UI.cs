using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    [Header("Menus")]
    public GameObject pauseMenu;
    public GameObject optionsMenu;

    [Header("Options UI")]
    public Toggle fullscreenToggle;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    private bool paused = false;

    private void Start() {
        // Load saved settings
        fullscreenToggle.isOn = Screen.fullScreen;
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Add event listeners
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        // Ensure menus start hidden
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
    }

    // Toggle pause menu
    public void TogglePauseMenu()
    {
        paused = !paused;

		// Pause/Resume
        if (paused) {
            Time.timeScale = 0f;
            pauseMenu.SetActive(true);
        } else {
            ResumeGame();
        }
    }

    // Resume the game
    public void ResumeGame()
    {
        paused = false;
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
    }

    // Open options menu
    public void OpenOptionsMenu() {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    // Close options menu (back to pause menu)
    public void OpenPauseMenu()
    {
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    // Exit the game
    public void ExitGame() {
        Application.Quit();
    }

    // Set Fullscreen Mode
    public void SetFullscreen(bool isFullscreen) {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Set Music Volume
    public void SetMusicVolume(float volume) {
        if (MusicManager.Instance != null) {
            MusicManager.Instance.SetMusicVolume(volume);
        }
    }

    // Set SFX Volume
    public void SetSFXVolume(float volume) {
        if (MusicManager.Instance != null) {
            MusicManager.Instance.SetSFXVolume(volume);
        }
    }
}
