using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static event Action<string> OnGameOver;
    public static event Action<int> OnOrbCountChanged;

    private int collectedOrbs = 0;
    private const int targetOrbs = 5;
    private bool isGameOver = false;

    private void OnEnable()
    {
        PlayerController.OnOrbCollected += HandleOrbCollected;
        PlayerController.OnPlayerDied += HandlePlayerDied;
        TimeManager.OnTimeOut += HandleTimeOut;
    }

    private void OnDisable()
    {
        PlayerController.OnOrbCollected -= HandleOrbCollected;
        PlayerController.OnPlayerDied -= HandlePlayerDied;
        TimeManager.OnTimeOut -= HandleTimeOut;
    }

    private void HandleOrbCollected()
    {
        // Don't count orbs if game already over
        if (isGameOver) return;

        collectedOrbs++;
        OnOrbCountChanged?.Invoke(collectedOrbs);

        if (collectedOrbs >= targetOrbs)
        {
            TriggerGameOver("Task Completed Successfully");
        }
    }

    private void HandlePlayerDied()
    {
        TriggerGameOver("You Died");
    }

    private void HandleTimeOut()
    {
        TriggerGameOver("Time out");
    }

    private void TriggerGameOver(string message)
    {
        if (isGameOver) return;
        isGameOver = true;
        OnGameOver?.Invoke(message);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
