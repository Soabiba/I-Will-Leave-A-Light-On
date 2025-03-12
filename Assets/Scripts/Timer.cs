using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class Timer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float gameTime = 120f; // 2 minutes as per your game design
    public TextMeshProUGUI timerText;
    public string gameOverSceneName = "GameOver";
    public float delayBeforeGameOver = 1f;

    private float currentTime;
    private bool isRunning = false;
    public float TimeProgress { get; private set; }

    void Start()
    {
        // Make sure we have a timer text component
        if (timerText == null)
        {
            Debug.LogError("Timer Text is not assigned to the Timer script!");
        }
        else
        {
            // Initialize timer display
            UpdateTimerDisplay();

            // Auto-start timer (remove this if you want to start manually)
            StartTimer();
        }
    }

    public void StartTimer()
    {
        currentTime = gameTime;
        TimeProgress = 0f;
        isRunning = true;
        Debug.Log("Timer started: " + gameTime + " seconds");
    }

    private void Update()
    {
        if (!isRunning) return;

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();
            TimeProgress = 1 - (currentTime / gameTime);
        }
        else
        {
            GameOver();
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void GameOver()
    {
        isRunning = false;
        currentTime = 0;

        if (timerText != null)
        {
            timerText.text = "00:00";
        }

        Debug.Log("Game Over! Loading scene: " + gameOverSceneName);
        StartCoroutine(LoadGameOverScene());
    }

    private IEnumerator LoadGameOverScene()
    {
        yield return new WaitForSeconds(delayBeforeGameOver);

        // Check if the scene exists in build settings
        if (SceneUtility.GetBuildIndexByScenePath(gameOverSceneName) >= 0)
        {
            SceneManager.LoadScene(gameOverSceneName);
        }
        else
        {
            Debug.LogError("Scene '" + gameOverSceneName + "' is not in build settings! Add it to build settings.");
        }
    }
}