using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Klak.Hap;

/// <summary>
/// Custom HAP video player that provides similar functionality to the CustomVideoPlayer
/// but uses the Klak.Hap.HapPlayer for video playback instead of Unity's VideoPlayer.
/// </summary>
[RequireComponent(typeof(HapPlayer))]
public class HapVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [Tooltip("Path to the video file (relative to StreamingAssets folder)")]
    [SerializeField] private string videoFileName;
    [Tooltip("Should the video loop when it reaches the end")]
    [SerializeField] private bool isLooping = false;
    [Tooltip("Current playback time (in seconds)")]
    [SerializeField] private float currentTime = 0f;
    [Tooltip("Total video duration (in seconds)")]
    [SerializeField] private float totalDuration = 0f;
    
    // Flag to track if video needs to be refreshed
    private string lastLoadedFileName;
    
    [Header("Fade Settings")]
    [Tooltip("Duration of fade in/out in seconds")]
    [Range(0f, 5f)]
    [SerializeField] private float fadeDuration = 1.0f;
    [Tooltip("Current alpha value of the video")]
    [Range(0f, 1f)]
    [SerializeField] private float currentAlphaProp = 0.0f;
    
    [Header("Rendering")]
    [SerializeField] private RenderTexture targetRenderTexture;
    
    [Header("References")]
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private CanvasGroup elementCanvasGroup;
    //[SerializeField] private AudioSource audioSource;
    
    // Private variables
    private HapPlayer hapPlayer;
    private float targetAlpha = 0.0f;
    private float currentAlpha = 0.0f;
    private bool isPrepared = false;
    private bool isPlaying = false;
    
    private void Awake()
    {
        // Get components
        hapPlayer = GetComponent<HapPlayer>();
        
        // if (audioSource == null)
        // {
        //     audioSource = GetComponent<AudioSource>();
        // }
        
        // Handle CanvasGroup initialization
        if (elementCanvasGroup == null && videoDisplay != null)
        {
            // Create a CanvasGroup on the same GameObject as the RawImage
            elementCanvasGroup = videoDisplay.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Set initial alpha to zero
        if (elementCanvasGroup != null)
        {
            elementCanvasGroup.alpha = 0;
            currentAlpha = 0;
            targetAlpha = 0;
        }
    }
    
    private void Start()
    {
        SetupVideoPlayer();
        
        // Store the initial filename
        lastLoadedFileName = videoFileName;
        
        // Ensure initial alpha is set to zero
        if (elementCanvasGroup != null)
        {
            elementCanvasGroup.alpha = 0;
            currentAlpha = 0;
            targetAlpha = 0;
        }
    }
    
    private void Update()
    {
        // Check if the filename has changed
        if (videoFileName != lastLoadedFileName)
        {
            if (Application.isPlaying)
            {
                RefreshVideo();
            }
        }
        
        // Handle fading
        if (elementCanvasGroup != null && currentAlpha != targetAlpha)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / fadeDuration);
            elementCanvasGroup.alpha = currentAlpha;
            
            // Update the inspector value for debugging
            currentAlphaProp = currentAlpha;
        }
        
        // Update current time and duration if player is valid
        if (isPrepared && hapPlayer.isValid && isPlaying)
        {
            // Get time from the player
            currentTime = hapPlayer.time;
            totalDuration = (float)hapPlayer.streamDuration;
            
            // Manual audio looping since HAP doesn't handle audio
            // if (audioSource != null && isPlaying)
            // {
            //     // Check if audio needs to be restarted for looping
            //     if (isLooping && !audioSource.isPlaying && 
            //         (audioSource.time >= audioSource.clip.length - 0.1f || audioSource.time == 0))
            //     {
            //         audioSource.Play();
            //     }
            // }
        }
    }
    
    private void SetupVideoPlayer()
    {
        if (string.IsNullOrEmpty(videoFileName)) return;
        
        // Ensure HAP player is available
        if (hapPlayer == null)
        {
            hapPlayer = GetComponent<HapPlayer>();
            if (hapPlayer == null)
            {
                Debug.LogError("HapPlayer component not found!");
                return;
            }
        }
        
        // Set looping
        hapPlayer.loop = isLooping;
        
        // Set up the render texture
        if (targetRenderTexture != null)
        {
            hapPlayer.targetTexture = targetRenderTexture;
            
            if (videoDisplay != null)
            {
                videoDisplay.texture = targetRenderTexture;
            }
        }
        
        // Open the video file
        hapPlayer.Open(videoFileName, HapPlayer.PathMode.StreamingAssets);
        
        // Check if the open was successful
        if (hapPlayer.isValid)
        {
            isPrepared = true;
            totalDuration = (float)hapPlayer.streamDuration;
            
            Debug.Log("HAP video player prepared: " + videoFileName);
        }
        else
        {
            Debug.LogError("Failed to open HAP video: " + videoFileName);
        }
    }
    
    // Refresh the video with a new filename
    public void RefreshVideo()
    {
        if (hapPlayer != null)
        {
            // Remember if we were playing
            bool wasPlaying = isPlaying;
            
            // Stop playback
            if (isPlaying)
            {
                Stop(false); // Stop without fade
            }
            
            // Reset time and isPrepared flag
            hapPlayer.time = 0;
            isPrepared = false;
            
            // Re-setup the player with the new file
            SetupVideoPlayer();
            
            // Update the last loaded filename
            lastLoadedFileName = videoFileName;
            
            // Resume playback if it was playing before
            if (wasPlaying && isPrepared)
            {
                Play();
            }
            
            Debug.Log("HAP video refreshed with new file: " + videoFileName);
        }
    }
    
    // Public method to set a new video filename and refresh
    public void SetVideoFileName(string newFileName)
    {
        if (videoFileName != newFileName)
        {
            videoFileName = newFileName;
            RefreshVideo();
        }
    }
    
    // Public control methods
    public void Play()
    {
        if (!isPlaying && isPrepared)
        {
            // Clear the render texture before playing
            ClearRenderTexture();
            
            isPlaying = true;
            
            // Start audio if available
            // if (audioSource != null && audioSource.clip != null)
            // {
            //     audioSource.Play();
            // }
            
            // Set playback speed to positive for forward playback
            hapPlayer.speed = 1.0f;
            hapPlayer.time = 0;

            // Ensure enabled
            hapPlayer.enabled = true;
            
            // Fade in the video
            FadeIn();
        }
    }
    
    // Clear the render texture
    private void ClearRenderTexture()
    {
        if (targetRenderTexture != null)
        {
            // Store current active render texture
            RenderTexture currentRT = RenderTexture.active;
            
            // Set our render texture as active
            RenderTexture.active = targetRenderTexture;
            
            // Clear it with transparent black
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            
            // Restore previous active render texture
            RenderTexture.active = currentRT;
        }
    }
    
    public void Pause()
    {
        if (isPlaying)
        {
            isPlaying = false;
            
            // Set playback speed to 0 to pause
            hapPlayer.speed = 0;
            
            // Pause audio if available
            // if (audioSource != null)
            // {
            //     audioSource.Pause();
            // }
        }
    }
    
    public void Stop(bool withFade = true)
    {
        if (isPlaying)
        {
            if (withFade)
            {
                StartCoroutine(StopWithFade());
            }
            else
            {
                isPlaying = false;
                hapPlayer.speed = 0;
                hapPlayer.time = 0;
                
                // Stop audio if available
                // if (audioSource != null)
                // {
                //     audioSource.Stop();
                // }
                
                // Set alpha to 0 immediately
                if (elementCanvasGroup != null)
                {
                    elementCanvasGroup.alpha = 0;
                    currentAlpha = 0;
                    targetAlpha = 0;
                    currentAlphaProp = 0;
                }
            }
        }
    }
    
    private IEnumerator StopWithFade()
    {
        FadeOut();
        yield return new WaitForSeconds(fadeDuration);
        
        isPlaying = false;
        hapPlayer.speed = 0;
        hapPlayer.time = 0;
        
        // Stop audio if available
        // if (audioSource != null)
        // {
        //     audioSource.Stop();
        // }
    }
    
    public void SetVolume(float volume)
    {
        // if (audioSource != null)
        // {
        //     audioSource.volume = Mathf.Clamp01(volume);
        // }
    }
    
    // Fade methods
    public void FadeIn()
    {
        targetAlpha = 1.0f;
    }
    
    public void FadeOut()
    {
        targetAlpha = 0.0f;
    }
    
    // Set alpha directly
    public void SetAlpha(float alpha)
    {
        float clampedAlpha = Mathf.Clamp01(alpha);
        currentAlpha = clampedAlpha;
        targetAlpha = clampedAlpha;
        currentAlphaProp = clampedAlpha;
        
        if (elementCanvasGroup != null)
        {
            elementCanvasGroup.alpha = clampedAlpha;
        }
    }
    
    // Toggle looping
    public void SetLooping(bool loop)
    {
        isLooping = loop;
        if (hapPlayer != null)
        {
            hapPlayer.loop = loop;
        }
    }
    
    // Seek to specific time
    // public void SeekTo(float timeInSeconds)
    // {
    //     if (hapPlayer != null && isPrepared)
    //     {
    //         hapPlayer.time = Mathf.Clamp(timeInSeconds, 0, (float)hapPlayer.streamDuration);
            
    //         // Sync audio if available
    //         if (audioSource != null && audioSource.clip != null)
    //         {
    //             audioSource.time = Mathf.Clamp(timeInSeconds, 0, audioSource.clip.length);
    //         }
    //     }
    // }
    
    // Get formatted time string (MM:SS)
    public string GetFormattedTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    // Get current time formatted
    public string GetCurrentTimeFormatted()
    {
        return GetFormattedTime(currentTime);
    }
    
    // Get total duration formatted
    public string GetTotalDurationFormatted()
    {
        return GetFormattedTime(totalDuration);
    }
    
    // Get progress as percentage (0-1)
    public float GetProgress()
    {
        if (totalDuration > 0)
            return currentTime / totalDuration;
        return 0;
    }
    
    // Check if the video is currently playing
    public bool IsPlaying()
    {
        return isPlaying;
    }
    
    // Check if the video is valid and prepared
    public bool IsPrepared()
    {
        return isPrepared && hapPlayer != null && hapPlayer.isValid;
    }
    
    public void UpdateNow()
    {
        if (hapPlayer != null)
        {
            hapPlayer.UpdateNow();
        }
    }
}