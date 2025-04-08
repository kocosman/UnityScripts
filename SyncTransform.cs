using UnityEngine;

/// <summary>
/// Synchronizes the transform of this GameObject with a reference GameObject.
/// Allows for individually toggling position, rotation, and scale synchronization.
/// </summary>
public class SyncTransform : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("The reference GameObject whose transform this object will match")]
    [SerializeField] private GameObject targetObject;

    [Header("Sync Settings")]
    [Tooltip("Whether to sync the position")]
    [SerializeField] private bool syncPosition = true;
    
    [Tooltip("Whether to sync the rotation")]
    [SerializeField] private bool syncRotation = true;
    
    [Tooltip("Whether to sync the scale")]
    [SerializeField] private bool syncScale = false;

    [Header("Offset Settings")]
    [Tooltip("Offset to apply to the position")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("Offset to apply to the rotation (in Euler angles)")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    
    [Tooltip("Multiplier to apply to the scale (1 = same scale)")]
    [SerializeField] private Vector3 scaleMultiplier = Vector3.one;

    [Header("Update Settings")]
    [Tooltip("Whether to sync in Update instead of LateUpdate")]
    [SerializeField] private bool useUpdate = true;

    [Header("Local vs World Settings")]
    [Tooltip("Use local transforms instead of world transforms")]
    [SerializeField] private bool useLocalTransform = false;

    private void Start()
    {
        // Validate the reference in Start
        if (targetObject == null)
        {
            Debug.LogWarning($"SyncTransform on {gameObject.name}: No target object assigned!", this);
        }
    }

    private void Update()
    {
        if (useUpdate)
        {
            SyncTransformWithTarget();
        }
    }

    private void LateUpdate()
    {
        if (!useUpdate)
        {
            SyncTransformWithTarget();
        }
    }

    private void SyncTransformWithTarget()
    {
        // Only proceed if we have a target
        if (targetObject == null) return;

        // Sync position if enabled
        if (syncPosition)
        {
            if (useLocalTransform)
            {
                transform.localPosition = targetObject.transform.localPosition + positionOffset;
            }
            else
            {
                transform.position = targetObject.transform.position + positionOffset;
            }
        }

        // Sync rotation if enabled
        if (syncRotation)
        {
            if (useLocalTransform)
            {
                transform.localRotation = targetObject.transform.localRotation * Quaternion.Euler(rotationOffset);
            }
            else
            {
                transform.rotation = targetObject.transform.rotation * Quaternion.Euler(rotationOffset);
            }
        }

        // Sync scale if enabled
        if (syncScale)
        {
            if (useLocalTransform)
            {
                Vector3 targetScale = targetObject.transform.localScale;
                transform.localScale = new Vector3(
                    targetScale.x * scaleMultiplier.x,
                    targetScale.y * scaleMultiplier.y,
                    targetScale.z * scaleMultiplier.z
                );
            }
            else
            {
                // Note: World scale is more complex, this is a simplification
                Vector3 targetLossyScale = targetObject.transform.lossyScale;
                Vector3 parentLossyScale = transform.parent ? transform.parent.lossyScale : Vector3.one;
                
                transform.localScale = new Vector3(
                    (targetLossyScale.x * scaleMultiplier.x) / parentLossyScale.x,
                    (targetLossyScale.y * scaleMultiplier.y) / parentLossyScale.y,
                    (targetLossyScale.z * scaleMultiplier.z) / parentLossyScale.z
                );
            }
        }
    }

    #region Public Methods

    /// <summary>
    /// Sets a new target object at runtime
    /// </summary>
    public void SetTargetObject(GameObject newTarget)
    {
        targetObject = newTarget;
    }

    /// <summary>
    /// Sets a new position offset at runtime
    /// </summary>
    public void SetPositionOffset(Vector3 newOffset)
    {
        positionOffset = newOffset;
    }

    /// <summary>
    /// Sets a new rotation offset at runtime
    /// </summary>
    public void SetRotationOffset(Vector3 newOffset)
    {
        rotationOffset = newOffset;
    }

    /// <summary>
    /// Sets a new scale multiplier at runtime
    /// </summary>
    public void SetScaleMultiplier(Vector3 newMultiplier)
    {
        scaleMultiplier = newMultiplier;
    }

    /// <summary>
    /// Enables or disables position synchronization
    /// </summary>
    public void SetSyncPosition(bool sync)
    {
        syncPosition = sync;
    }

    /// <summary>
    /// Enables or disables rotation synchronization
    /// </summary>
    public void SetSyncRotation(bool sync)
    {
        syncRotation = sync;
    }

    /// <summary>
    /// Enables or disables scale synchronization
    /// </summary>
    public void SetSyncScale(bool sync)
    {
        syncScale = sync;
    }

    /// <summary>
    /// Switches between using local and world transforms
    /// </summary>
    public void SetUseLocalTransform(bool useLocal)
    {
        useLocalTransform = useLocal;
    }

    /// <summary>
    /// Toggles position synchronization
    /// </summary>
    public void ToggleSyncPosition()
    {
        syncPosition = !syncPosition;
    }

    /// <summary>
    /// Toggles rotation synchronization
    /// </summary>
    public void ToggleSyncRotation()
    {
        syncRotation = !syncRotation;
    }

    /// <summary>
    /// Toggles scale synchronization
    /// </summary>
    public void ToggleSyncScale()
    {
        syncScale = !syncScale;
    }

    #endregion
}