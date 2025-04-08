using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float duration = 60f;
    [SerializeField] private bool autoStart = true;
    
    [Header("References")]
    [SerializeField] private Material timerMaterial;
    [SerializeField] private TextMeshProUGUI timeText;  // Changed to TextMeshProUGUI
    
    private float currentTime;
    private bool isRunning;
    
    // Property to expose remaining time
    public float RemainingTime => currentTime;
    
    // Property to check if timer is active
    public bool IsRunning => isRunning;

    private void Start()
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
        
        // Update text display
        if (timeText != null)
        {
            timeText.text = $"{Mathf.Ceil(currentTime):0}";
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
        // Add any completion logic here
        Debug.Log("Timer Complete!");
    }
}