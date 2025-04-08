using UnityEngine;

/// <summary>
/// Synchronizes the position of this GameObject with a reference GameObject.
/// Ensures both objects maintain the same position at all times.
/// </summary>
public class SyncPosition : MonoBehaviour
{
    [Tooltip("The reference GameObject whose position this object will match")]
    [SerializeField] private GameObject targetObject;

    [Tooltip("Optional offset to apply to the position")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Tooltip("Whether to sync position in Update or LateUpdate")]
    [SerializeField] private bool useUpdate = true;

    private void Start()
    {
        // Validate the reference in Start
        if (targetObject == null)
        {
            Debug.LogWarning($"SyncPosition on {gameObject.name}: No target object assigned!", this);
        }
    }

    private void Update()
    {
        if (useUpdate)
        {
            SyncPositionWithTarget();
        }
    }

    private void LateUpdate()
    {
        if (!useUpdate)
        {
            SyncPositionWithTarget();
        }
    }

    private void SyncPositionWithTarget()
    {
        // Only proceed if we have a target
        if (targetObject == null) return;

        // Update this object's position to match the target's
        transform.position = targetObject.transform.position + positionOffset;
    }

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
}