using UnityEngine;
using System.IO;
using System.Collections;

[ExecuteAlways] // Enables editor refresh behavior
public class RuntimeTextureLoader : MonoBehaviour
{
    [Header("Texture Settings")]
    public string textureAddress = "image.png";        // Target texture address
    public string fallbackTextureAddress = "default.png";  // Fallback texture

    [Header("References")]
    public WedgeBehavior targetWedgeBehavior;          // Reference to the WedgeBehavior to update
    public ExploreModeImageDatabase imageDatabase;         // Reference to the AmbientImageDatabase

    [Header("Runtime Control")]
    public bool refreshInInspector = false;            // Toggle to trigger refresh in Inspector
    public bool useNextImageFromDatabase = true;       // Whether to get the next image from database when refreshing

    private Texture2D loadedTexture;

    void Awake()
    {
        // Check for required components
        if (targetWedgeBehavior == null)
        {
            targetWedgeBehavior = GetComponent<WedgeBehavior>();
            if (targetWedgeBehavior == null)
            {
                Debug.LogWarning("No WedgeBehavior assigned or found on this GameObject!");
            }
        }
    }

    void OnEnable()
    {
        // LoadTextureByAddress(textureAddress);
        Refresh();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            Refresh();
        }
    }

    void Update()
    {
        // Trigger refresh from inspector toggle
        if (refreshInInspector || Input.GetKeyDown(KeyCode.R))
        {
            refreshInInspector = false;
            Refresh();
        }
    }

    /// <summary>
    /// Call this from other scripts to refresh the texture
    /// </summary>
    public void Refresh()
    {
        if (useNextImageFromDatabase && imageDatabase != null)
        {
            // Get the next image address from the database
            if (imageDatabase.imageWedgeContent != null && imageDatabase.imageWedgeContent.Length > 0)
            {
                // Let the database manage which image to use next
                int currentIndex = System.Array.IndexOf(imageDatabase.imageWedgeContent, textureAddress);
                
                // Get next image from database's content
                textureAddress = GetNextImageFromDatabase(currentIndex);
            }
            else
            {
                Debug.LogWarning("Image database has no content!");
            }
        }

        LoadTextureAndApply(textureAddress);
    }

    /// <summary>
    /// Gets the next image address from the AmbientImageDatabase based on the current index
    /// </summary>
    private string GetNextImageFromDatabase(int currentIndex)
    {
        if (imageDatabase == null || imageDatabase.imageWedgeContent == null || imageDatabase.imageWedgeContent.Length == 0)
        {
            Debug.Log("**Image database is not properly configured!");
            return fallbackTextureAddress;
        }
        
        string nextImageAddress = imageDatabase.GetNextImage();
        
        // Check if the address is valid
        if (string.IsNullOrEmpty(nextImageAddress))
        {
            Debug.Log("**Empty image address in database!");
            return fallbackTextureAddress;
        }

        return nextImageAddress;
    }

    /// <summary>
    /// Loads the texture from the given address and applies it to the WedgeBehavior
    /// </summary>
    private void LoadTextureAndApply(string address)
    {
        string path = Path.Combine(Application.streamingAssetsPath, address);
        
        if (!File.Exists(path))
        {
            Debug.Log($"**Texture not found at: {path}, loading fallback.");
            path = Path.Combine(Application.streamingAssetsPath, fallbackTextureAddress);

            if (!File.Exists(path))
            {
                Debug.LogError("Fallback texture also not found!");
                return;
            }
        }
        Debug.Log("**LoadingCurrentTextureOnEnable** " + textureAddress);

        StartCoroutine(LoadTexture(path));
    }

    private IEnumerator LoadTexture(string path)
    {
        using (WWW www = new WWW("file://" + path))
        {
            yield return www;
            
            if (string.IsNullOrEmpty(www.error))
            {
                // Create a new texture if needed or reset the existing one
                if (loadedTexture == null)
                {
                    loadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                }
                
                www.LoadImageIntoTexture(loadedTexture);
                
                // Apply the texture to the WedgeBehavior
                if (targetWedgeBehavior != null)
                {
                    // Update the wedge behavior to use the image type and apply our texture
                    targetWedgeBehavior.Type = WedgeType.Image;
                    
                    // Set the texture using reflection since fillTexture is private in WedgeBehavior
                    var fieldInfo = typeof(WedgeBehavior).GetField("fillTexture", 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Public);
                    
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(targetWedgeBehavior, loadedTexture);
                        Debug.Log($"Texture loaded and applied successfully: {Path.GetFileName(path)}");
                    }
                    else
                    {
                        Debug.LogError("Could not access fillTexture field on WedgeBehavior!");
                    }
                }
                else
                {
                    Debug.LogError("No target WedgeBehavior to apply texture to!");
                }
            }
            else
            {
                Debug.LogError($"Failed to load texture: {www.error}");
            }
        }
    }

    /// <summary>
    /// Public method to load a specific texture by address
    /// </summary>
    public void LoadTextureByAddress(string address)
    {
        if (!string.IsNullOrEmpty(address))
        {
            textureAddress = address;
            LoadTextureAndApply(textureAddress);
        }
    }
}