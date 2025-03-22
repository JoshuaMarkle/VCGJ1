using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class UI : MonoBehaviour
{
	public static UI Instance;

	[Header("Menus")]
	public GameObject mainMenu;
	public GameObject pauseMenu;
	public GameObject optionsMenu;
	public GameObject gameOverMenu;

	[Header("Options UI")]
	public Slider musicVolumeSlider;
	public Slider sfxVolumeSlider;

	[Header("HUD")]
	public TMP_Text cashText;
	public TMP_Text pizzaText;
	public TMP_Text policeStarsText;
	public Slider hungerSlider;

	[Header("Game Over")]
	public TMP_Text gameOverSubtitle;

	[Header("Fade Panel")]
	public GameObject panel;
	private CanvasGroup panelGroup;
	public float fadeTime = 1f;

	private bool pausable = true;
	private bool paused = false;

	private void Awake()
	{
		if (Instance == null) Instance = this;
		else Destroy(gameObject);
	}

	private void Start()
	{
		// Fade panel setup
		if (panel != null)
		{
			panelGroup = panel.GetComponent<CanvasGroup>();
			if (panelGroup == null)
			{
				panelGroup = panel.AddComponent<CanvasGroup>();
			}
			panelGroup.alpha = 1f;
			panel.SetActive(true);
			StartCoroutine(FadeIn());
		}

		// Load saved settings
		if (musicVolumeSlider != null)
		{
			musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
			musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
		}

		if (sfxVolumeSlider != null)
		{
			sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
			sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
		}

		if (mainMenu != null) mainMenu.SetActive(true);
		if (pauseMenu != null) pauseMenu.SetActive(false);
		if (optionsMenu != null) optionsMenu.SetActive(false);
		if (gameOverMenu != null) gameOverMenu.SetActive(false);

		UpdateHUD();
	}


	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			TogglePauseMenu();
		}

		UpdateHUD();
	}


	public void TogglePauseMenu()
	{
		if (!pausable) return;

		paused = !paused;

		if (paused)
		{
			Time.timeScale = 0f;
			if (pauseMenu != null) pauseMenu.SetActive(true);
		}
		else
		{
			ResumeGame();
		}
	}

	public void ResumeGame()
	{
		paused = false;
		Time.timeScale = 1f;
		if (pauseMenu != null) pauseMenu.SetActive(false);
	}

	public void OpenOptionsMenu()
	{
		if (mainMenu != null) mainMenu.SetActive(false);
		if (pauseMenu != null) pauseMenu.SetActive(false);
		if (pauseMenu != null) pauseMenu.SetActive(false);
		if (optionsMenu != null) optionsMenu.SetActive(true);
	}

	public void OpenPauseMenu()
	{
		if (mainMenu != null) mainMenu.SetActive(false);
		if (optionsMenu != null) optionsMenu.SetActive(false);
		if (pauseMenu != null) pauseMenu.SetActive(true);
	}

	public void OpenMainMenu()
	{
		if (mainMenu != null) mainMenu.SetActive(true);
		if (optionsMenu != null) optionsMenu.SetActive(false);
		if (pauseMenu != null) pauseMenu.SetActive(false);
	}

	public void UpdateHUD()
	{
		if (GameMaster.Instance == null) return;

		if (cashText != null)
			cashText.text = $"${GameMaster.Instance.cash}";

		if (pizzaText != null)
			pizzaText.text = $"P{GameMaster.Instance.pizzasInCar}";

		if (policeStarsText != null)
			policeStarsText.text = new string('g', GameMaster.Instance.policeStars);

		if (hungerSlider != null)
			hungerSlider.value = GameMaster.Instance.hunger;
	}

	public IEnumerator FadeIn()
	{
		if (panel == null || panelGroup == null) yield break;

		float timer = 0f;
		panel.SetActive(true);
		while (timer < fadeTime)
		{
			timer += Time.deltaTime;
			panelGroup.alpha = 1f - (timer / fadeTime);
			yield return null;
		}
		panelGroup.alpha = 0f;
		panel.SetActive(false);
	}

	public IEnumerator FadeOut()
	{
		if (panel == null || panelGroup == null) yield break;

		panel.SetActive(true);
		float timer = 0f;
		while (timer < fadeTime)
		{
			timer += Time.deltaTime;
			panelGroup.alpha = timer / fadeTime;
			yield return null;
		}
		panelGroup.alpha = 1f;
	}

	public void ExitGame()
	{
		Application.Quit();
	}

	public void ToggleFullscreen()
	{
		bool isFullscreen = !Screen.fullScreen;
		Screen.fullScreen = isFullscreen;
		PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
		PlayerPrefs.Save();
	}

	public void SetMusicVolume(float volume)
	{
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.SetMusicVolume(volume);
		}
	}

	public void SetSFXVolume(float volume)
	{
		if (MusicManager.Instance != null)
		{
			MusicManager.Instance.SetSFXVolume(volume);
		}
	}

	public void LoadScene(string sceneName)
	{
		StartCoroutine(FadeOutAndLoadScene(sceneName));
	}

	private IEnumerator FadeOutAndLoadScene(string sceneName)
	{
		yield return StartCoroutine(FadeOut());
		SceneManager.LoadScene(sceneName);
	}

	public void ShowGameOverScreen(string subtitle) {
		pausable = false;
		if (gameOverMenu != null) gameOverMenu.SetActive(true);
		gameOverSubtitle.text = subtitle;
	}
}
