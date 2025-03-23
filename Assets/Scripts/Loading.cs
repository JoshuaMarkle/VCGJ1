using UnityEngine;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    [Header("Loading Settings")]
    public AudioClip loadingSound;
    public float loadDelay = 1f; // Extra time after the sound before scene load
    public string sceneToLoad = "Game";

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = loadingSound;
        audioSource.playOnAwake = false;

        if (loadingSound != null)
        {
            audioSource.Play();
            StartCoroutine(WaitAndLoad());
        }
        else
        {
            Debug.LogWarning("Loading.cs: No loadingSound assigned.");
            Invoke(nameof(LoadScene), loadDelay); // Fallback if no sound
        }
    }

    private System.Collections.IEnumerator WaitAndLoad()
    {
        // Wait for the sound to finish
        yield return new WaitForSeconds(loadingSound.length + loadDelay);
        LoadScene();
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
