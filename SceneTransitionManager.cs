using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    // Singleton instance
    public static SceneTransitionManager Instance { get; private set; }

   // public static string SceneToLoadAfterLoadingScreen = null;
   // [SerializeField] private string loadingSceneName = "LoadingScene"; // Add this to your header fields

    [Header("Scene Names")]
    [SerializeField] private string ambientSceneName = "Ambient";
    [SerializeField] private string exploreSceneName = "Explore";
    [SerializeField] private string takeoverSceneName = "Takeover";
    [SerializeField] private string photoSceneName = "Photo";

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool transitioning = false;

    // Reference to transition overlay
    private Canvas transitionCanvas;
    private CanvasGroup fadeCanvasGroup;

    #if UNITY_EDITOR
    // These flags are used by the editor script to trigger scene changes
    [HideInInspector] public bool loadAmbient = false;
    [HideInInspector] public bool loadExplore = false;
    [HideInInspector] public bool loadTakeover = false;
    [HideInInspector] public bool loadPhoto = false;
    #endif

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Debug.Log("Duplicate SceneTransitionManager found, destroying this instance");
            Destroy(gameObject);
            return;
        }
        
        // Set the static instance to this instance
        Instance = this;
        
        // Make sure this object persists between scene loads
        DontDestroyOnLoad(gameObject);

        // Create transition canvas
        CreateTransitionCanvas();
    }

    private void OnDestroy()
    {
        // Clean up the singleton reference when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        #if UNITY_EDITOR
        // Handle editor transition triggers
        if (loadAmbient)
        {
            loadAmbient = false;
            LoadAmbientScene();        
            }
        else if (loadExplore)
        {
            loadExplore = false;
            LoadExploreScene();
        }
        else if (loadTakeover)
        {
            loadTakeover = false;
            LoadTakeoverScene();
        }
        else if (loadPhoto)
        {
            loadPhoto = false;
            LoadPhotoScene();
        }
        #endif
    }

/*
    public void TransitionToSceneWithLoading(string targetSceneName)
    {
        if (transitioning)
        {
            Debug.LogWarning("Already transitioning. Ignoring request.");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target scene is null or empty.");
            return;
        }

        LoadingData.SceneToLoad = targetSceneName;

        Debug.Log("SceneToLoad set to: " + LoadingData.SceneToLoad);

        StartCoroutine(TransitionCoroutine(loadingSceneName));
    }
*/

    private void CreateTransitionCanvas()
    {
        // Create a canvas that stays above everything for the fade effect
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);
        
        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 999; // Make sure it renders on top
        
        // Add a canvas scaler
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add canvas group for fading
        fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0;
        fadeCanvasGroup.blocksRaycasts = false;
        
        // Add a full-screen image for the fade
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        var image = imageObj.AddComponent<UnityEngine.UI.Image>();
        image.color = fadeColor;
        
        // Set the image to fill the screen
        var rectTransform = imageObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Transitions to the specified scene with a fade effect
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        if (transitioning)
        {
            Debug.LogWarning("Already transitioning to a scene. Ignoring request.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is null or empty. Cannot transition.");
            return;
        }

        // Check if the scene exists in build settings
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameFromPath == sceneName)
            {
                sceneExists = true;
                break;
            }
        }

        if (!sceneExists)
        {
            Debug.LogError($"Scene '{sceneName}' not found in build settings. Make sure to add it to the build settings.");
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        transitioning = true;

        if (showDebugInfo)
            Debug.Log($"Starting transition to {sceneName}");

        // Ensure the fade canvas is active and at the front
        transitionCanvas.gameObject.SetActive(true);
        transitionCanvas.sortingOrder = 999;

        // Start fully transparent
        fadeCanvasGroup.alpha = 0;
        fadeCanvasGroup.blocksRaycasts = true;

        // Fade to black (current scene fades out)
        yield return StartCoroutine(FadeToColor());

        // Load the scene (behind the fade overlay)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = true;
        
        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Give the new scene a moment to initialize
        yield return new WaitForSeconds(0.1f);

        // Fade from black (new scene fades in)
        yield return StartCoroutine(FadeFromColor());

        // Disable blocking raycasts when fully faded out
        fadeCanvasGroup.blocksRaycasts = false;

        if (showDebugInfo)
            Debug.Log($"Completed transition to {sceneName}");

        transitioning = false;
    }

    private IEnumerator FadeToColor()
    {
        float elapsedTime = 0;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / transitionDuration);
            float curveValue = transitionCurve.Evaluate(normalizedTime);
            
            fadeCanvasGroup.alpha = curveValue;
            
            yield return null;
        }

        // Ensure we're fully opaque at the end
        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeFromColor()
    {
        float elapsedTime = 0;

        // Start fully opaque
        fadeCanvasGroup.alpha = 1f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / transitionDuration);
            float curveValue = transitionCurve.Evaluate(normalizedTime);
            
            // Invert the curve value to fade from the color
            fadeCanvasGroup.alpha = 1f - curveValue;
            
            yield return null;
        }

        // Ensure we're fully transparent at the end
        fadeCanvasGroup.alpha = 0f;
    }
    
    #region Public Scene Transition Methods
    
    public void LoadAmbientScene()
    {
        TransitionToScene(ambientSceneName);
    }
    
    public void LoadExploreScene()
    {
        TransitionToScene(exploreSceneName);
    }
    
    public void LoadTakeoverScene()
    {
        TransitionToScene(takeoverSceneName);
    }

    public void LoadPhotoScene()
    {
        TransitionToScene(photoSceneName);
    }
    
    #endregion
}