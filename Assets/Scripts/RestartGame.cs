using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestartButton : MonoBehaviour
{
    [SerializeField] private Button restartButton;
    public string sceneName = "Main";

    private void Start()
    {
        // Ensure the button is assigned either in inspector or find it programmatically
        if (restartButton == null)
            restartButton = GetComponent<Button>();

        // Add listener to the button
        restartButton.onClick.AddListener(RestartGame);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(sceneName);
    }
}