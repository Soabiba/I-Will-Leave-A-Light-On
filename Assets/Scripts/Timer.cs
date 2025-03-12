// Timer.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Timer : MonoBehaviour
{
    public float gameTime = 180f;
    public TextMeshProUGUI timerText;
    public string gameOverSceneName = "GameOver";
    public float delayBeforeGameOver = 1f;

    private float currentTime;
    private bool isRunning = false;
    public float TimeProgress { get; private set; }

    public void StartTimer()
    {
        currentTime = gameTime;
        TimeProgress = 0f;
        isRunning = true;
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
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void GameOver()
    {
        isRunning = false;
        currentTime = 0;
        timerText.text = "00:00";
        StartCoroutine(LoadGameOverScene());
    }

    private System.Collections.IEnumerator LoadGameOverScene()
    {
        yield return new WaitForSeconds(delayBeforeGameOver);
        SceneManager.LoadScene(gameOverSceneName);
    }
}