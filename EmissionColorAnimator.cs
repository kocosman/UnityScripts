using UnityEngine;

/// <summary>
/// Animates the emission color intensity of a material using an animation curve.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class EmissionColorAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The animation curve that controls how the intensity changes over time")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("The duration of one full animation cycle in seconds")]
    [SerializeField] private float duration = 1.0f;
    
    [Tooltip("Whether the animation should loop")]
    [SerializeField] private bool loop = true;
    
    [Tooltip("The base emission color (intensity will be applied to this)")]
    [SerializeField] private Color baseEmissionColor = Color.white;
    
    [Tooltip("The maximum intensity multiplier")]
    [SerializeField] private float maxIntensity = 1.0f;
    
    [Header("Material Settings")]
    [Tooltip("The material index to animate (for renderers with multiple materials)")]
    [SerializeField] private int materialIndex = 0;
    
    [Tooltip("The property name of the emission color")]
    [SerializeField] private string emissionColorProperty = "_EmissionColor";

    private Renderer targetRenderer;
    private Material targetMaterial;
    private float currentTime = 0f;
    private bool isAnimating = true;
    
    // The original emission color (used as a reference point)
    private Color originalEmissionColor;

    private void Awake()
    {
        // Get the renderer component
        targetRenderer = GetComponent<Renderer>();
        
        if (targetRenderer == null)
        {
            Debug.LogError("EmissionColorAnimator: No Renderer component found!");
            enabled = false;
            return;
        }
        
        // Make sure the material exists and is accessible
        if (targetRenderer.sharedMaterials.Length <= materialIndex)
        {
            Debug.LogError($"EmissionColorAnimator: Material index {materialIndex} is out of range!");
            enabled = false;
            return;
        }
        
        // Create instance of the material to avoid modifying the shared material
        targetMaterial = new Material(targetRenderer.sharedMaterials[materialIndex]);
        
        // Save the original emission color
        if (targetMaterial.HasProperty(emissionColorProperty))
        {
            originalEmissionColor = targetMaterial.GetColor(emissionColorProperty);
            
            // Apply the material instance to the renderer
            Material[] materials = targetRenderer.materials;
            materials[materialIndex] = targetMaterial;
            targetRenderer.materials = materials;
        }
        else
        {
            Debug.LogError($"EmissionColorAnimator: Material doesn't have property '{emissionColorProperty}'!");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // Reset animation when enabled
        currentTime = 0f;
        isAnimating = true;
    }

    private void Update()
    {
        if (!isAnimating || targetMaterial == null)
            return;

        // Update time
        currentTime += Time.deltaTime;
        
        // Calculate normalized time (0 to 1)
        float normalizedTime = (duration <= 0) ? 0 : (currentTime % duration) / duration;
        
        // If not looping and we've completed one cycle, stop animating
        if (!loop && currentTime >= duration)
        {
            normalizedTime = 1f;
            isAnimating = false;
        }
        
        // Get the intensity from the curve
        float intensity = intensityCurve.Evaluate(normalizedTime) * maxIntensity;
        
        // Apply the intensity to the emission color
        Color emissionColor = baseEmissionColor * intensity;
        
        // Set the emission color
        targetMaterial.SetColor(emissionColorProperty, emissionColor);
    }

    /// <summary>
    /// Starts or resumes the animation.
    /// </summary>
    public void Play()
    {
        isAnimating = true;
    }

    /// <summary>
    /// Pauses the animation.
    /// </summary>
    public void Pause()
    {
        isAnimating = false;
    }

    /// <summary>
    /// Stops the animation and resets to the beginning.
    /// </summary>
    public void Stop()
    {
        isAnimating = false;
        currentTime = 0f;
        
        // Reset emission color to original
        if (targetMaterial != null && targetMaterial.HasProperty(emissionColorProperty))
        {
            targetMaterial.SetColor(emissionColorProperty, originalEmissionColor);
        }
    }

    /// <summary>
    /// Sets a new base emission color.
    /// </summary>
    public void SetBaseEmissionColor(Color newColor)
    {
        baseEmissionColor = newColor;
    }

    /// <summary>
    /// Sets a new maximum intensity.
    /// </summary>
    public void SetMaxIntensity(float newMaxIntensity)
    {
        maxIntensity = Mathf.Max(0f, newMaxIntensity);
    }

    /// <summary>
    /// Sets whether the animation should loop.
    /// </summary>
    public void SetLoop(bool shouldLoop)
    {
        loop = shouldLoop;
    }

    /// <summary>
    /// Sets a new animation duration in seconds.
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = Mathf.Max(0.001f, newDuration);
    }

    private void OnDestroy()
    {
        // Clean up the material instance if it exists
        if (targetMaterial != null)
        {
            // If in the editor and not playing, destroy immediately
            if (!Application.isPlaying)
            {
                DestroyImmediate(targetMaterial);
            }
            else
            {
                Destroy(targetMaterial);
            }
        }
    }
}