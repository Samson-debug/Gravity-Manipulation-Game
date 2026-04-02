using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static event Action<int> OnTimeChanged;
    public static event Action OnTimeOut;

    [Header("Time Settings")]
    public float timeLimit = 120f;

    private float timeLeft;
    private bool isTimerRunning = true;
    private int lastReportedTime = -1;

    private void OnEnable()
    {
        GameManager.OnGameOver += StopTimer;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= StopTimer;
    }

    private void Start()
    {
        timeLeft = timeLimit;
        
        ReportTime();
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        timeLeft -= Time.deltaTime;
        
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            isTimerRunning = false;
            ReportTime();
            OnTimeOut?.Invoke();
        }
        else
        {
            ReportTime();
        }
    }

    private void ReportTime()
    {
        int currentTimeInt = Mathf.CeilToInt(timeLeft);
        if (currentTimeInt != lastReportedTime)
        {
            lastReportedTime = currentTimeInt;
            OnTimeChanged?.Invoke(currentTimeInt);
        }
    }

    private void StopTimer(string unusedMessage)
    {
        isTimerRunning = false;
    }
}
