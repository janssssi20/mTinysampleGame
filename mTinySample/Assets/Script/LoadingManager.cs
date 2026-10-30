using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Settings")]
    [Tooltip("Minimum time in seconds the loading screen will stay visible, even if the scene loads instantly.")]
    [SerializeField] private float minimumLoadTime = 1f;

    private void Awake()
    {
        // Scene-scoped: the newest scene's manager takes over. Never destroy
        // the GameObject here; it carries other managers too.

        Instance = this;
    }

    private bool isLoading = false;

    public void LoadScene(string sceneName)
    {
        // Ignore repeated clicks while a scene is already loading
        if (isLoading)
        {
            return;
        }

        isLoading = true;

        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private System.Collections.IEnumerator LoadSceneAsync(string sceneName)
    {
        // 1. Activate the loading screen UI
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Loading Screen Panel is not assigned in the LoadingManager inspector!");
        }

        // Start loading asynchronously and block immediate activation
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.unscaledDeltaTime;

            // Unity progress caps at 0.9 until allowSceneActivation is true
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Blend real load progress with the minimum timer so the bar moves smoothly
            float displayProgress = Mathf.Min(loadProgress, timer / minimumLoadTime);

            // Update UI elements
            if (progressBar != null)
            {
                progressBar.value = displayProgress;
            }

            if (progressText != null)
            {
                progressText.text = (displayProgress * 100f).ToString("F0") + "%";
            }

            // Check if both the scene is ready (0.9) AND the minimum time has elapsed
            if (operation.progress >= 0.9f && timer >= minimumLoadTime)
            {
                // Verification: Ensure progress bar hits 100% right before switching
                if (progressBar != null) progressBar.value = 1f;
                if (progressText != null) progressText.text = "100%";

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}