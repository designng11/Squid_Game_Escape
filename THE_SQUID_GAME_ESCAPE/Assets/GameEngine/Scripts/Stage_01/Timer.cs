using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private float currentTime = 0f;
    public float GameTime = 0;
    public Text GameTimeText;
    private bool isTimerRunning = false;

    void Update()
    {
        if (isTimerRunning)
        {
            currentTime += Time.deltaTime;
            // Update timer display or trigger events based on currentTime
        }
    }

    public void StartTimer()
    {
        isTimerRunning = true;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void ResetTimer()
    {
        currentTime = 0f;
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }
}