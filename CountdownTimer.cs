using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float duration = 60f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool useMinutesFormat = true; // true for mm:ss, false for ss
    
    [Header("Events")]
    [SerializeField] private UnityEvent onTimerComplete = new UnityEvent();
    
    [Header("References")]
    [SerializeField] private Material timerMaterial;
    [SerializeField] private TextMeshProUGUI[] timeText;  // Array of text components
    
    private float currentTime;
    private bool isRunning;
    
    // Properties to expose values
    public float RemainingTime => currentTime;
    public bool IsRunning => isRunning;
    public float Duration => duration;

    private void Start()
    {
        if (autoStart)
        {
            ResetTimer();
        }
    }

    private void OnEnable()
    {
        if (autoStart)
        {
            ResetTimer();
        }
    }

    private void Update()
    {
        if (!isRunning) return;
        
        // Update timer
        currentTime -= Time.deltaTime;
        
        // Check if timer completed
        if (currentTime <= 0)
        {
            currentTime = 0;
            isRunning = false;
            OnTimerComplete();
        }
        
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        // Update material
        if (timerMaterial != null)
        {
            float normalizedTime = currentTime / duration;
            timerMaterial.SetFloat("_Percentage", normalizedTime);
        }
        
        // Update text displays
        if (timeText != null && timeText.Length > 0)
        {
            string timeString;
            
            if (useMinutesFormat)
            {
                // mm:ss format
                int totalSeconds = Mathf.CeilToInt(currentTime);
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                timeString = $"{minutes:00}:{seconds:00}";
            }
            else
            {
                // ss format
                timeString = $"{Mathf.CeilToInt(currentTime)}";
            }
            
            for (int i = 0; i < timeText.Length; i++)
            {
                if (timeText[i] != null)
                {
                    timeText[i].text = timeString;
                }
            }
        }
    }
    
    public void ResetTimer()
    {
        currentTime = duration;
        isRunning = true;
        UpdateVisuals();
    }
    
    public void PauseTimer()
    {
        isRunning = false;
    }
    
    public void ResumeTimer()
    {
        if (currentTime > 0)
        {
            isRunning = true;
        }
    }
    
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        ResetTimer();
    }
    
    private void OnTimerComplete()
    {
        // Invoke the UnityEvent to trigger attached functions
        onTimerComplete?.Invoke();
        Debug.Log("Timer Complete!");
    }
}
