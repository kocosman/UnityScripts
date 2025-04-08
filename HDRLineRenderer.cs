using UnityEngine;
using Shapes2D;

/// <summary>
/// Renders a line with HDR color capabilities between two objects.
/// </summary>
[ExecuteInEditMode]
public class HDRLineRenderer : MonoBehaviour
{
    [Header("Line Endpoints")]
    [SerializeField] private Transform startObject;
    [SerializeField] private Transform endObject;
    
    [Header("Line Appearance")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private float intensity = 1.5f; // HDR intensity multiplier
    [SerializeField] private bool useHDR = true;
    
    [Header("Shape References")]
    [SerializeField] private Shape lineShape;
    
    // Offset for line endpoints
    [SerializeField] private Vector3 startOffset = Vector3.zero;
    [SerializeField] private Vector3 endOffset = Vector3.zero;
    
    private Vector3 lastStartPosition;
    private Vector3 lastEndPosition;
    private bool initialized = false;

    private void Awake()
    {
        Initialize();
    }
    
    private void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
        
        UpdateLine();
    }
    
    private void Update()
    {
        if (startObject == null || endObject == null) return;
        
        // Check if positions have changed
        if (startObject.position + startOffset != lastStartPosition || 
            endObject.position + endOffset != lastEndPosition)
        {
            UpdateLine();
        }
    }
    
    private void Initialize()
    {
        // Create line shape if not assigned
        if (lineShape == null)
        {
            lineShape = GetComponent<Shape>();
            
            if (lineShape == null)
            {
                lineShape = gameObject.AddComponent<Shape>();
                lineShape.settings.shapeType = ShapeType.Rectangle;
                lineShape.settings.fillType = FillType.SolidColor;
                lineShape.Configure();
            }
        }
        
        initialized = true;
    }
    
    private void UpdateLine()
    {
        if (startObject == null || endObject == null || lineShape == null) return;
        
        // Get endpoint positions with offset
        Vector3 startPosition = startObject.position + startOffset;
        Vector3 endPosition = endObject.position + endOffset;
        
        // Calculate line properties
        Vector3 direction = endPosition - startPosition;
        float distance = direction.magnitude;
        
        // Update transform position to be in the middle
        transform.position = startPosition + (direction / 2f);
        
        // Calculate rotation to point from start to end
        transform.rotation = Quaternion.LookRotation(direction);
        transform.Rotate(Vector3.right, 90f); // Rotate to match plane orientation
        
        // Scale based on distance and width
        transform.localScale = new Vector3(lineWidth, distance, 1f);
        
        // Apply HDR color
        Color hdrColor = lineColor;
        if (useHDR)
        {
            // Multiply RGB by intensity to simulate HDR
            hdrColor = new Color(
                lineColor.r * intensity,
                lineColor.g * intensity,
                lineColor.b * intensity,
                lineColor.a
            );
        }
        
        lineShape.settings.fillColor = hdrColor;
        
        // Update cached positions
        lastStartPosition = startPosition;
        lastEndPosition = endPosition;
    }
    
    /// <summary>
    /// Set the start and end objects for the line
    /// </summary>
    public void SetLineEndpoints(Transform start, Transform end)
    {
        startObject = start;
        endObject = end;
        UpdateLine();
    }
    
    /// <summary>
    /// Set the color of the line with optional HDR intensity
    /// </summary>
    public void SetLineColor(Color color, float hdrIntensity = 1.5f)
    {
        lineColor = color;
        intensity = hdrIntensity;
        UpdateLine();
    }
    
    /// <summary>
    /// Set the width of the line
    /// </summary>
    public void SetLineWidth(float width)
    {
        lineWidth = width;
        UpdateLine();
    }
    
    /// <summary>
    /// Set offsets for the line endpoints
    /// </summary>
    public void SetLineOffsets(Vector3 start, Vector3 end)
    {
        startOffset = start;
        endOffset = end;
        UpdateLine();
    }
}