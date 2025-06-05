using UnityEngine;
using UnityEngine.UI;
using System.IO;

[ExecuteAlways] // Enables editor refresh behavior
public class RuntimeImageLoader : MonoBehaviour
{
    [Header("Image Settings")]
    public string fileName = "image.png";                // Target image
    public string fallbackFileName = "default.png";      // Fallback image

    [Header("Display Settings")]
    [Tooltip("When enabled, sets the RawImage to its native size after loading")]
    public bool useNativeSize = false;

    [Header("Runtime Control")]
    public bool refreshInInspector = false;              // Toggle to trigger refresh in Inspector

    private RawImage rawImage;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
           // LoadImage(fileName);
        }
    }

    void OnEnable(){
        Refresh();
    }

    void Update()
    {
        // Trigger refresh from inspector toggle
        if (refreshInInspector)
        {
            refreshInInspector = false;
            Refresh();
        }
    }

    /// <summary>
    /// Call this from other scripts after changing fileName
    /// </summary>
    public void Refresh()
    {
        Debug.Log("Refreshing image with filename: " + fileName);
        LoadImage(fileName);
    }

    /// <summary>
    /// Change the image file and immediately refresh
    /// </summary>
    public void SetImage(string newFileName)
    {
        fileName = newFileName;
        Refresh();
    }

    public void SetImageAddress(string newFileName)
    {
        fileName = newFileName;
    }

    private void LoadImage(string imageName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, imageName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Image not found at: {path}, loading fallback.");
            path = Path.Combine(Application.streamingAssetsPath, fallbackFileName);

            if (!File.Exists(path))
            {
                Debug.LogError("Fallback image also not found.");
                return;
            }
        }

        StartCoroutine(LoadTexture(path));
    }

    private System.Collections.IEnumerator LoadTexture(string path)
    {
        using (WWW www = new WWW("file://" + path))
        {
            yield return www;
            if (string.IsNullOrEmpty(www.error))
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                www.LoadImageIntoTexture(tex);
                
                if (rawImage != null)
                {
                    // Assign the loaded texture
                    rawImage.texture = tex;
                    
                    // If useNativeSize is enabled, set the RawImage to use the texture's native size
                    if (useNativeSize)
                    {
                        rawImage.SetNativeSize();
                    }
                }
            }
            else
            {
                Debug.LogError($"Failed to load texture: {www.error}");
            }
        }
    }
    
    /// <summary>
    /// Gets the currently loaded texture
    /// </summary>
    public Texture2D GetCurrentTexture()
    {
        return rawImage?.texture as Texture2D;
    }
    
    /// <summary>
    /// Sets the useNativeSize property and applies it immediately if a texture is loaded
    /// </summary>
    public void SetUseNativeSize(bool value)
    {
        useNativeSize = value;
        
        // If we're switching to native size and we already have a texture loaded, apply immediately
        if (useNativeSize && rawImage != null && rawImage.texture != null)
        {
            rawImage.SetNativeSize();
        }
    }
}