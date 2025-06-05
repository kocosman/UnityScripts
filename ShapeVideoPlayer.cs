using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using Shapes2D;
using UnityEngine.Events;

[System.Serializable]
public class VideoFadeCompleteEvent : UnityEvent { }

/// <summary>
/// Video player component for Shapes2D that outputs video to a Shape's fill texture.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class ShapeVideoPlayer : MonoBehaviour
{

    [Header("Events")]
    [SerializeField] // Make sure this is serialized
    public VideoFadeCompleteEvent onFadeOutComplete = new VideoFadeCompleteEvent();

    [Header("Video Settings")]
    [Tooltip("Path to the video file (relative to StreamingAssets folder)")]
    [SerializeField] private string videoFileName;
    [Tooltip("Should the video loop when it reaches the end")]
    [SerializeField] private bool isLooping = false;
    [SerializeField] private bool isPlayOnAwake = false;
    [SerializeField] private float currentTime = 0f;
    [SerializeField] private float totalDuration = 0f;
    
    [Header("Rendering")]
    [Tooltip("RenderTexture to use for video output")]
    [SerializeField] private RenderTexture renderTexture;
    [Tooltip("Automatically create a RenderTexture if none is specified")]
    [SerializeField] private bool autoCreateRenderTexture = true;
    [SerializeField] private int renderTextureWidth = 512;
    [SerializeField] private int renderTextureHeight = 512;
    
    [Header("Shape Reference")]
    [Tooltip("Reference to the Shape component that will display the video")]
    [SerializeField] private Shape targetShape;
    [Tooltip("Find the Shape component on this GameObject if not specified")]
    [SerializeField] private bool useLocalShape = true;
    
    [Header("Fade Settings")]
    [Range(0f, 5f)]
    [SerializeField] private float fadeDuration = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float currentAlpha = 0.0f;
    
    [SerializeField] private bool debugMode = true;


    // Private variables
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private float targetAlpha = 0.0f;
    private Texture2D videoTexture;
    private bool isSetup = false;
    private Color originalFillColor;
    private bool originalFillColorStored = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // Get components
        videoPlayer = GetComponent<VideoPlayer>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // If no target shape specified and useLocalShape is true, try to find a Shape component on this GameObject
        if (targetShape == null && useLocalShape)
        {
            targetShape = GetComponent<Shape>();
        }
        
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = isPlayOnAwake;
        }
    }
    
    private void Start()
    {
        SetupVideoPlayer();
    }
    
    private void OnDisable()
    {
        // Reset the shape's fill type and color if we had stored it
        if (targetShape != null && originalFillColorStored)
        {
            targetShape.settings.fillColor = originalFillColor;
        }
    }
    
    private void Update()
    {
        // Handle fading
        if (currentAlpha != targetAlpha)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / fadeDuration);
            
            // Apply alpha to the shape if it exists
            if (targetShape != null)
            {
                Color fillColor = targetShape.settings.fillColor;
                fillColor.a = currentAlpha;
                targetShape.settings.fillColor = fillColor;
                
                // Also fade the sprite renderer if it exists
                if (spriteRenderer != null)
                {
                    Color spriteColor = spriteRenderer.color;
                    spriteColor.a = currentAlpha;
                    spriteRenderer.color = spriteColor;
                }
            }
            else if (spriteRenderer != null)
            {
                // If no shape, but we have a sprite renderer, fade that
                Color spriteColor = spriteRenderer.color;
                spriteColor.a = currentAlpha;
                spriteRenderer.color = spriteColor;
            }
        }
        
        // Update current time and duration if video is prepared
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            currentTime = (float)videoPlayer.time;
            totalDuration = (float)videoPlayer.length;
            
            // Check if we need to start fading out before the end
            if (videoPlayer.isPlaying && !isLooping && 
                currentTime >= (totalDuration - fadeDuration) && 
                currentAlpha > 0)
            {
                FadeOut();
                
                // If we're very close to the end, stop the video
                if (currentTime >= (totalDuration - 0.1f))
                {
                    videoPlayer.Stop();
                    videoPlayer.time = 0;
                            onFadeOutComplete.Invoke();

                }
            }
        }
    }
    
    private void SetupVideoPlayer()
    {
        if (string.IsNullOrEmpty(videoFileName) || isSetup)
            return;

        // Setup video path
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        videoPlayer.url = videoPath;
        
        // Set looping
        videoPlayer.isLooping = isLooping;
        
        // Create RenderTexture if needed
        if (renderTexture == null && autoCreateRenderTexture)
        {
            renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0, RenderTextureFormat.ARGB32);
            renderTexture.Create();
        }
        
        // Create a Texture2D that will receive the video frame data
        videoTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        
        // Configure video output
        if (renderTexture != null)
        {
            videoPlayer.targetTexture = renderTexture;
            
            // Configure the target shape if available
            if (targetShape != null)
            {
                // Store the original fill color for later restoration
                originalFillColor = targetShape.settings.fillColor;
                originalFillColorStored = true;
                
                // Set the shape's fill type to texture
                targetShape.settings.fillType = FillType.Texture;
                targetShape.settings.fillTexture = videoTexture;
                
                // Initialize with proper alpha
                Color fillColor = targetShape.settings.fillColor;
                fillColor.a = currentAlpha;
                targetShape.settings.fillColor = fillColor;
            }
            else if (spriteRenderer != null)
            {
                // Store original color
                originalFillColor = spriteRenderer.color;
                originalFillColorStored = true;
                
                // Initialize with proper alpha
                Color spriteColor = spriteRenderer.color;
                spriteColor.a = currentAlpha;
                spriteRenderer.color = spriteColor;
            }
            else
            {
                Debug.LogWarning("No target Shape or SpriteRenderer specified. Video will render to texture but won't be displayed.");
            }
        }
        
        // Link audio source if available
        if (audioSource != null)
        {
            videoPlayer.SetTargetAudioSource(0, audioSource);
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.enabled = true;
        }
        
        // Register for preparation complete event
        videoPlayer.prepareCompleted += VideoPlayerPrepared;
        videoPlayer.Prepare();
        
        isSetup = true;
    }
    
    private void VideoPlayerPrepared(VideoPlayer source)
    {
        Debug.Log("ShapeVideoPlayer: Video player prepared - " + videoFileName);
        Play();
    }
    
    private void UpdateVideoTexture()
    {
        if (renderTexture != null && videoTexture != null)
        {
            // Store active render texture
            RenderTexture currentRT = RenderTexture.active;
            
            // Set render texture as active
            RenderTexture.active = renderTexture;
            
            // Read pixels from render texture to the texture2D
            videoTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            videoTexture.Apply();
            
            // Restore previous active render texture
            RenderTexture.active = currentRT;
            
            // If we have a sprite renderer but no shape, set the sprite renderer's texture
            if (targetShape == null && spriteRenderer != null)
            {
                // Create a new sprite from the texture if needed
                if (spriteRenderer.sprite == null || spriteRenderer.sprite.texture != videoTexture)
                {
                    Sprite newSprite = Sprite.Create(videoTexture, 
                                                    new Rect(0, 0, videoTexture.width, videoTexture.height), 
                                                    new Vector2(0.5f, 0.5f));
                    spriteRenderer.sprite = newSprite;
                }
            }
        }
    }
    
    private void LateUpdate()
    {
        if (videoPlayer != null && videoPlayer.isPlaying && renderTexture != null)
        {
            UpdateVideoTexture();
        }
    }
    
    // Public control methods
    
    /// <summary>
    /// Plays the video and fades it in.
    /// </summary>
    public void Play()
    {
        if (!videoPlayer.isPlaying)
        {
            // Make sure the video is not about to end
            if (videoPlayer.isPrepared && (float)videoPlayer.time >= ((float)videoPlayer.length - fadeDuration * 1.1f))
            {
                videoPlayer.time = 0;
            }
            
            // Clear any previous frame that might be lingering
            ClearRenderTexture();
            
            videoPlayer.Play();
            FadeIn();
        }
    }
    
    /// <summary>
    /// Pauses the video playback.
    /// </summary>
    public void Pause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }
    
    /// <summary>
    /// Stops the video with a fade out.
    /// </summary>
    public void Stop()
    {
        if (videoPlayer.isPlaying)
        {
            StartCoroutine(StopWithFade());
        }
    }
    
private IEnumerator StopWithFade()
{
    // Start the fade out
    FadeOut();
    
    // Wait for the fade duration
    yield return new WaitForSeconds(fadeDuration);
    
    // Stop the video player
    videoPlayer.Stop();
    videoPlayer.time = 0;
    
    // Clear the render texture
    ClearRenderTexture();
    
    // IMPORTANT: Make sure we're properly invoking the event
    // First debug log to verify we reached this point
    Debug.Log("Video fade-out complete, about to trigger event");
    
    // Make sure the event is not null and has listeners
    if (onFadeOutComplete != null)
    {
        // Get the listener count for debugging
        int listenerCount = onFadeOutComplete.GetPersistentEventCount();
        Debug.Log($"Event has {listenerCount} persistent listeners");
        
        // Invoke the event - this is the critical line
        onFadeOutComplete.Invoke();
        Debug.Log("Fade complete event invoked");
    }
    else
    {
        Debug.LogError("onFadeOutComplete event is null!");
    }
    
    // Fallback - try to directly find and call the controller
    var controller = FindObjectOfType<AnimationVideoController>();
    if (controller != null)
    {
        Debug.Log("Found AnimationVideoController, calling directly");
        // Call the public method directly as a fallback
        controller.OnVideoFadeOutComplete();
    }
}
    
    /// <summary>
    /// Sets the volume of the associated audio source.
    /// </summary>
    /// <param name="volume">Volume level between 0 and 1</param>
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    /// <summary>
    /// Fades in the video.
    /// </summary>
    public void FadeIn()
    {
        targetAlpha = 1.0f;
    }
    
    /// <summary>
    /// Fades out the video.
    /// </summary>
    public void FadeOut()
    {
        targetAlpha = 0.0f;
    }
    
    /// <summary>
    /// Sets the alpha directly.
    /// </summary>
    /// <param name="alpha">Alpha value between 0 and 1</param>
    public void SetAlpha(float alpha)
    {
        float clampedAlpha = Mathf.Clamp01(alpha);
        currentAlpha = clampedAlpha;
        targetAlpha = clampedAlpha;
        
        // Apply to shape immediately
        if (targetShape != null)
        {
            Color fillColor = targetShape.settings.fillColor;
            fillColor.a = clampedAlpha;
            targetShape.settings.fillColor = fillColor;
        }
        
        // Apply to sprite renderer if available
        if (spriteRenderer != null)
        {
            Color spriteColor = spriteRenderer.color;
            spriteColor.a = clampedAlpha;
            spriteRenderer.color = spriteColor;
        }
    }
    
    /// <summary>
    /// Sets the looping behavior.
    /// </summary>
    /// <param name="loop">Whether the video should loop</param>
    public void SetLooping(bool loop)
    {
        isLooping = loop;
        if (videoPlayer != null)
        {
            videoPlayer.isLooping = loop;
        }
    }
    
    /// <summary>
    /// Clears the render texture to prevent the last frame from persisting.
    /// </summary>
    private void ClearRenderTexture()
    {
        if (renderTexture != null)
        {
            // Store current active render texture
            RenderTexture currentRT = RenderTexture.active;
            
            // Set our render texture as active
            RenderTexture.active = renderTexture;
            
            // Clear the render texture with transparent black
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            
            // If we have a video texture, clear that too
            if (videoTexture != null)
            {
                // Create a transparent texture
                Color[] clearColors = new Color[videoTexture.width * videoTexture.height];
                for (int i = 0; i < clearColors.Length; i++)
                {
                    clearColors[i] = new Color(0, 0, 0, 0);
                }
                
                // Apply the transparent colors to the texture
                videoTexture.SetPixels(clearColors);
                videoTexture.Apply();
            }
            
            // Restore previous render texture
            RenderTexture.active = currentRT;
        }
    }
    
    /// <summary>
    /// Seeks to a specific time in the video.
    /// </summary>
    /// <param name="timeInSeconds">Time in seconds to seek to</param>
    public void SeekTo(float timeInSeconds)
    {
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            videoPlayer.time = Mathf.Clamp(timeInSeconds, 0, (float)videoPlayer.length);
        }
    }
    
    /// <summary>
    /// Changes the video file.
    /// </summary>
    /// <param name="fileName">New video file name (relative to StreamingAssets)</param>
    public void ChangeVideo(string fileName)
    {
        if (videoPlayer != null)
        {
            bool wasPlaying = videoPlayer.isPlaying;
            videoPlayer.Stop();
            
            videoFileName = fileName;
            string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
            videoPlayer.url = videoPath;
            
            // Re-prepare the player
            isSetup = false;
            videoPlayer.prepareCompleted += (source) => {
                if (wasPlaying)
                {
                    Play();
                }
            };
            videoPlayer.Prepare();
        }
    }
    
    /// <summary>
    /// Sets a new target Shape to display the video.
    /// </summary>
    /// <param name="shape">New target Shape</param>
    public void SetTargetShape(Shape shape)
    {
        // Restore original settings if we had a previous target
        if (targetShape != null && originalFillColorStored)
        {
            targetShape.settings.fillColor = originalFillColor;
        }
        
        // Set the new target
        targetShape = shape;
        
        if (targetShape != null)
        {
            // Store the original fill color
            originalFillColor = targetShape.settings.fillColor;
            originalFillColorStored = true;
            
            // Configure the shape
            targetShape.settings.fillType = FillType.Texture;
            targetShape.settings.fillTexture = videoTexture;
            
            // Apply current alpha
            Color fillColor = targetShape.settings.fillColor;
            fillColor.a = currentAlpha;
            targetShape.settings.fillColor = fillColor;
        }
    }
    
    /// <summary>
    /// Sets a new SpriteRenderer to display the video.
    /// </summary>
    /// <param name="renderer">New target SpriteRenderer</param>
    public void SetSpriteRenderer(SpriteRenderer renderer)
    {
        // Store reference to the new sprite renderer
        spriteRenderer = renderer;
        
        if (spriteRenderer != null && videoTexture != null)
        {
            // Create sprite from the video texture
            Sprite newSprite = Sprite.Create(videoTexture, 
                                            new Rect(0, 0, videoTexture.width, videoTexture.height), 
                                            new Vector2(0.5f, 0.5f));
            spriteRenderer.sprite = newSprite;
            
            // Apply current alpha
            Color spriteColor = spriteRenderer.color;
            spriteColor.a = currentAlpha;
            spriteRenderer.color = spriteColor;
        }
    }
    
    /// <summary>
    /// Gets the current progress as a percentage (0-1).
    /// </summary>
    /// <returns>Current progress as a value between 0 and 1</returns>
    public float GetProgress()
    {
        if (totalDuration > 0)
            return currentTime / totalDuration;
        return 0;
    }
    
    /// <summary>
    /// Gets the formatted time string (MM:SS).
    /// </summary>
    /// <param name="timeInSeconds">Time in seconds</param>
    /// <returns>Formatted time string</returns>
    public string GetFormattedTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    /// <summary>
    /// Gets the current time as a formatted string.
    /// </summary>
    /// <returns>Current time as MM:SS</returns>
    public string GetCurrentTimeFormatted()
    {
        return GetFormattedTime(currentTime);
    }
    
    /// <summary>
    /// Gets the total duration as a formatted string.
    /// </summary>
    /// <returns>Total duration as MM:SS</returns>
    public string GetTotalDurationFormatted()
    {
        return GetFormattedTime(totalDuration);
    }
    
    /// <summary>
    /// Refreshes the video by forcing a texture update.
    /// </summary>
    public void RefreshVideo()
    {
        if (renderTexture != null && videoTexture != null)
        {
            UpdateVideoTexture();
        }
    }
    
    // Properties for easy inspector visibility
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;
    public bool IsPrepared => videoPlayer != null && videoPlayer.isPrepared;
}