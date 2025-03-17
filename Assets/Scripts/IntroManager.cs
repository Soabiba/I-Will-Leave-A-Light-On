using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    public Canvas introCanvas;
    public Image introPanel;    // Changed from Panel to Image

    void Start()
    {
        // Pause the game
        Time.timeScale = 0f;

        // Make sure everything is visible
        introCanvas.enabled = true;
        introPanel.enabled = true;    // Changed from gameObject.SetActive

        // Start the coroutine to disable everything after delay
        StartCoroutine(DisableIntroScreen());
    }

    IEnumerator DisableIntroScreen()
    {
        // Wait for 2 seconds (real time, not game time)
        yield return new WaitForSecondsRealtime(4f);

        // Disable everything
        introPanel.enabled = false;    // Changed from gameObject.SetActive
        introCanvas.enabled = false;

        // Resume the game
        Time.timeScale = 1f;
    }
}