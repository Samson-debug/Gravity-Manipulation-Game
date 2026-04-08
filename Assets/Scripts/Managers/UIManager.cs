using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    public GameObject playerHud;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI orbCountText;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;

    private void OnEnable()
    {
        TimeManager.OnTimeChanged += UpdateTimerDisplay;
        GameManager.OnOrbCountChanged += UpdateOrbDisplay;
        GameManager.OnGameOver += ShowGameOverPanel;
    }

    private void OnDisable()
    {
        TimeManager.OnTimeChanged -= UpdateTimerDisplay;
        GameManager.OnOrbCountChanged -= UpdateOrbDisplay;
        GameManager.OnGameOver -= ShowGameOverPanel;
    }

    private void Start()
    {
        if (playerHud) playerHud.SetActive(true);
        
        if (gameOverPanel)
            gameOverPanel.SetActive(false);
        
        UpdateOrbDisplay(0);
    }

    private void UpdateTimerDisplay(int totalSeconds)
    {
        if (timerText)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void UpdateOrbDisplay(int count)
    {
        if (orbCountText) orbCountText.text = count.ToString();
    }

    private void ShowGameOverPanel(string message)
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);

        if (gameOverText) gameOverText.text = message;

        if (playerHud) playerHud.SetActive(false);

        // Free cursor for selecting restart button (in case previously locked)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
