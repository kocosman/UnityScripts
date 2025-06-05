using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(VideoPlayer))]
public class CustomVideoPlayer : MonoBehaviour
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
    [Tooltip("Choose rendering method")]
    [SerializeField] private RenderMethod renderMethod = RenderMethod.RenderTexture;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Material videoMaterial;
    
    [Header("References")]
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private CanvasGroup elementCanvasGroup;
    
    // Private variables
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private float targetAlpha = 0.0f;
    private float currentAlpha = 0.0f;
    
    // Enums
    public enum RenderMethod { RenderTexture, Material }
    
    private void Awake()
    {
        // Get components
        videoPlayer = GetComponent<VideoPlayer>();
        audioSource = GetComponent<AudioSource>();
        
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
        
        // Disable autoplay
        videoPlayer.playOnAwake = false;
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
        
        // Clear the render texture initially
        ClearRenderTexture();
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
            
            // Clear render texture when we finish fading out
            if (currentAlpha == 0 && targetAlpha == 0)
            {
                ClearRenderTexture();
            }
        }
        
        // Update current time and duration
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            currentTime = (float)videoPlayer.time;
            totalDuration = (float)videoPlayer.length;
        }
    }
    
    private void SetupVideoPlayer()
    {
        if (string.IsNullOrEmpty(videoFileName)) return;
        
        // Setup video path
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        videoPlayer.url = videoPath;
        
        // Set looping
        videoPlayer.isLooping = isLooping;
        
        // Disable autoplay
        videoPlayer.playOnAwake = false;
        
        // Setup output based on render method
        if (renderMethod == RenderMethod.RenderTexture && renderTexture != null)
        {
            videoPlayer.targetTexture = renderTexture;
            if (videoDisplay != null)
            {
                videoDisplay.texture = renderTexture;
            }
        }
        else if (renderMethod == RenderMethod.Material && videoMaterial != null)
        {
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.targetMaterialRenderer = GetComponent<Renderer>();
            videoPlayer.targetMaterialProperty = "_MainTex";
        }
        
        // Link audio source
        if (audioSource != null)
        {
            videoPlayer.SetTargetAudioSource(0, audioSource);
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.enabled = true;
        }
        
        // Prepare player
        videoPlayer.prepareCompleted += VideoPlayerPrepared;
        videoPlayer.Prepare();
    }
    
    private void VideoPlayerPrepared(VideoPlayer source)
    {
        Debug.Log("Video player prepared: " + videoFileName);
    }
    
    // Clear the render texture to prevent the last frame from persisting
    private void ClearRenderTexture()
    {
        if (renderTexture != null)
        {
            // Store current active render texture
            RenderTexture currentActiveRT = RenderTexture.active;
            
            // Set our render texture as active
            RenderTexture.active = renderTexture;
            
            // Clear the render texture with transparent black
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            
            // Restore the previously active render texture
            RenderTexture.active = currentActiveRT;
        }
    }
    
    // Refresh the video with a new filename
    public void RefreshVideo()
    {
        if (videoPlayer != null)
        {
            // Stop the current video if it's playing
            bool wasPlaying = videoPlayer.isPlaying;
            videoPlayer.Stop();
            
            // Clear current references
            videoPlayer.targetTexture = null;
            videoPlayer.url = "";
            
            // Clear the render texture
            ClearRenderTexture();
            
            // Set up the video player with the new file
            SetupVideoPlayer();
            
            // Update the last loaded filename
            lastLoadedFileName = videoFileName;
            
            // Resume playback if it was playing before
            if (wasPlaying && videoPlayer.isPrepared)
            {
                videoPlayer.Play();
            }
            
            Debug.Log("Video refreshed with new file: " + videoFileName);
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
                    ClearRenderTexture();

        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            FadeIn();
        }
    }
    
    public void Pause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }
    
    public void Stop()
    {
        if (videoPlayer.isPlaying)
        {
            StartCoroutine(StopWithFade());
        }
    }
    
    private IEnumerator StopWithFade()
    {
        FadeOut();
        yield return new WaitForSeconds(fadeDuration);
        videoPlayer.Stop();
        videoPlayer.time = 0;
        
        // Clear the render texture after stopping
        ClearRenderTexture();
    }
    
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
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
        
        // Clear render texture if we're setting alpha to 0
        if (clampedAlpha == 0)
        {
            ClearRenderTexture();
        }
    }
    
    // Toggle looping
    public void SetLooping(bool loop)
    {
        isLooping = loop;
        videoPlayer.isLooping = loop;
    }
    
    // Seek to specific time
    public void SeekTo(float timeInSeconds)
    {
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            videoPlayer.time = Mathf.Clamp(timeInSeconds, 0, (float)videoPlayer.length);
        }
    }
    
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
    
    // Clear render texture when disabled to prevent residual frames
    private void OnDisable()
    {
        ClearRenderTexture();
    }
    
    // Clear render texture when destroyed
    private void OnDestroy()
    {
        ClearRenderTexture();
    }
}