using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Controls the animation of a single TextMeshPro element that performs
/// fade in/out and slide animations when enabled.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TextMeshProBehavior : MonoBehaviour
{
    [System.Serializable]
    public class AnimationCompleteEvent : UnityEvent { }

    [Header("Animation Settings")]
    [SerializeField] private float startDelay = 0.0f; // Delay before animation starts
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float slideAmount = 100f;
    [SerializeField] private SlideDirection slideDirection = SlideDirection.Right;
    
    [Header("Animation Controls")]
    [SerializeField] private bool pauseAtMidpoint = false; // Field for pause feature

    [Header("Animation Curves")]
    [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.8f, 1, 1.0f);

    [Header("Events")]
    public AnimationCompleteEvent onAnimationComplete = new AnimationCompleteEvent();

    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    private Vector2 initialScale = new Vector2(1.0f, 1.0f);
    private TMP_Text tmpText;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Coroutine animationCoroutine;
    private bool isDelaying = false;
    private bool isPaused = false; // Tracks if animation is currently paused

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        rectTransform = tmpText.rectTransform;
        originalPosition = rectTransform.anchoredPosition;
        initialScale = rectTransform.localScale;
        
        // Initialize with zero alpha
        Color startColor = tmpText.color;
        startColor.a = 0f;
        tmpText.color = startColor;
    }

    private void OnEnable()
    {
        // Start animation when object is enabled
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        animationCoroutine = StartCoroutine(AnimateText());
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        // Reset position and scale for next time
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = Vector3.one;
        isPaused = false;
    }

    private IEnumerator AnimateText()
    {
        // Calculate total animation duration for scale curve mapping
        float totalDuration = fadeInDuration + displayDuration + fadeOutDuration;
        
        // Apply start delay if needed
        if (startDelay > 0f)
        {
            isDelaying = true;
            
            // During delay, ensure text is invisible
            Color color = tmpText.color;
            color.a = 0f;
            tmpText.color = color;
            
            // Set initial scale
            rectTransform.localScale = initialScale;
            
            // Wait for the delay duration
            yield return new WaitForSeconds(startDelay);
            isDelaying = false;
        }
        
        // Set starting position for slide in
        Vector2 startPosition = GetSlideStartPosition(originalPosition);
        rectTransform.anchoredPosition = startPosition;
        
        // Set initial alpha
        Color textColor = tmpText.color;
        textColor.a = 0f;
        tmpText.color = textColor;
        
        // Set initial scale
        rectTransform.localScale = initialScale;

        // Phase 1: Fade in with slide and scale
        float startTime = 0f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedFadeTime = Mathf.Clamp01(elapsed / fadeInDuration);
            
            // Apply separate curves for slide and fade
            float slideProgress = slideInCurve.Evaluate(normalizedFadeTime);
            float fadeProgress = fadeInCurve.Evaluate(normalizedFadeTime);
            
            // Calculate global progress for scale curve
            float globalProgress = Mathf.Clamp01((startTime + elapsed) / totalDuration);
            float scaleProgress = scaleCurve.Evaluate(globalProgress);
            
            // Update alpha with fade curve
            textColor.a = Mathf.Lerp(0f, 1f, fadeProgress);
            tmpText.color = textColor;
            
            // Update position with slide curve
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, originalPosition, slideProgress);
            
            // Update scale with scale curve
            Vector2 currentScale = initialScale * scaleProgress;
            rectTransform.localScale = new Vector3(currentScale.x, currentScale.y, 1f);
            
            yield return null;
        }
        
        // Ensure fade-in phase ends at target values
        textColor.a = 1f;
        tmpText.color = textColor;
        rectTransform.anchoredPosition = originalPosition;
        
        // Phase 2: Display duration with scale
        // If pauseAtMidpoint is true, pause here until isPaused is set to false
        if (pauseAtMidpoint)
        {
            isPaused = true;
            
            // Wait until unpaused
            while (isPaused)
            {
                yield return null;
            }
        }
        
        startTime = fadeInDuration;
        elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            
            // Calculate global progress for scale curve
            float globalProgress = Mathf.Clamp01((startTime + elapsed) / totalDuration);
            float scaleProgress = scaleCurve.Evaluate(globalProgress);
            
            // Update scale with scale curve
            Vector2 currentScale = initialScale * scaleProgress;
            rectTransform.localScale = new Vector3(currentScale.x, currentScale.y, 1f);
            
            yield return null;
        }
        
        // Phase 3: Fade out with slide and scale
        startTime = fadeInDuration + displayDuration;
        elapsed = 0f;
        Vector2 endPosition = GetSlideEndPosition(originalPosition);
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedFadeTime = Mathf.Clamp01(elapsed / fadeOutDuration);
            
            // Apply separate curves for slide and fade
            float slideProgress = slideOutCurve.Evaluate(normalizedFadeTime);
            float fadeProgress = fadeOutCurve.Evaluate(normalizedFadeTime);
            
            // Calculate global progress for scale curve
            float globalProgress = Mathf.Clamp01((startTime + elapsed) / totalDuration);
            float scaleProgress = scaleCurve.Evaluate(globalProgress);
            
            // Update alpha with fade curve
            textColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
            tmpText.color = textColor;
            
            // Update position with slide curve
            rectTransform.anchoredPosition = Vector2.Lerp(originalPosition, endPosition, slideProgress);
            
            // Update scale with scale curve
            Vector2 currentScale = initialScale * scaleProgress;
            rectTransform.localScale = new Vector3(currentScale.x, currentScale.y, 1f);
            
            yield return null;
        }
        
        // Ensure fade-out phase ends at target values
        textColor.a = 0f;
        tmpText.color = textColor;
        rectTransform.anchoredPosition = endPosition;
        
        // Notify that animation is complete
        onAnimationComplete.Invoke();
        
        // Reset pause state
        isPaused = false;
        
        // Disable the GameObject
        gameObject.SetActive(false);
    }

    private Vector2 GetSlideStartPosition(Vector2 originalPosition)
    {
        switch (slideDirection)
        {
            case SlideDirection.Left:
                return originalPosition + new Vector2(slideAmount, 0);
            case SlideDirection.Right:
                return originalPosition + new Vector2(-slideAmount, 0);
            case SlideDirection.Up:
                return originalPosition + new Vector2(0, -slideAmount);
            case SlideDirection.Down:
                return originalPosition + new Vector2(0, slideAmount);
            default:
                return originalPosition;
        }
    }
    
    private Vector2 GetSlideEndPosition(Vector2 originalPosition)
    {
        switch (slideDirection)
        {
            case SlideDirection.Left:
                return originalPosition + new Vector2(-slideAmount, 0);
            case SlideDirection.Right:
                return originalPosition + new Vector2(slideAmount, 0);
            case SlideDirection.Up:
                return originalPosition + new Vector2(0, slideAmount);
            case SlideDirection.Down:
                return originalPosition + new Vector2(0, -slideAmount);
            default:
                return originalPosition;
        }
    }

    /// <summary>
    /// Play the animation with a custom start delay
    /// </summary>
    /// <param name="customDelay">Delay in seconds before animation starts</param>
    public void PlayWithDelay(float customDelay)
    {
        // Stop any current animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // Set the custom delay
        startDelay = Mathf.Max(0f, customDelay);
        
        // Reset pause state
        isPaused = false;
        
        // Start the animation
        animationCoroutine = StartCoroutine(AnimateText());
    }

    /// <summary>
    /// Get or set the start delay value
    /// </summary>
    public float StartDelay
    {
        get { return startDelay; }
        set { startDelay = Mathf.Max(0f, value); }
    }

    /// <summary>
    /// Check if the text is currently in its delay phase
    /// </summary>
    public bool IsDelaying
    {
        get { return isDelaying; }
    }
    
    /// <summary>
    /// Sets whether to pause at the midpoint of the animation.
    /// </summary>
    public bool PauseAtMidpoint
    {
        get { return pauseAtMidpoint; }
        set { pauseAtMidpoint = value; }
    }

    /// <summary>
    /// Gets whether the animation is currently paused.
    /// </summary>
    public bool IsPaused
    {
        get { return isPaused; }
    }

    /// <summary>
    /// Resumes the animation if it's currently paused.
    /// </summary>
    public void Resume()
    {
        isPaused = false;
    }

    /// <summary>
    /// Forces the animation to pause, regardless of position.
    /// </summary>
    public void Pause()
    {
        isPaused = true;
    }
    
    /// <summary>
    /// Set the text content of the TextMeshPro component
    /// </summary>
    /// <param name="text">The text to display</param>
    public void SetText(string text)
    {
        if (tmpText != null)
        {
            tmpText.text = text;
        }
    }
    
    /// <summary>
    /// Set the color of the TextMeshPro component
    /// </summary>
    /// <param name="color">The color to set</param>
    public void SetColor(Color color)
    {
        if (tmpText != null)
        {
            // Preserve the current alpha
            Color newColor = color;
            newColor.a = tmpText.color.a;
            tmpText.color = newColor;
        }
    }
    

}