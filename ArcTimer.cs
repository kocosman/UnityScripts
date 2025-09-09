using UnityEngine;

public class ArcTimer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private ArcInstance arcInstance;
    
    private float initialAngle;
    
    private void Start()
    {
        // Store the initial angle from the ArcInstance
        if (arcInstance != null)
        {
            initialAngle = arcInstance.angle;
        }
    }
    
    private void Update()
    {
        // Update the arc angle based on timer progress
        if (countdownTimer != null && arcInstance != null)
        {
            float normalizedTime = countdownTimer.RemainingTime / countdownTimer.Duration;
            float currentAngle = initialAngle * normalizedTime;
            arcInstance.angle = currentAngle;
        }
    }
}