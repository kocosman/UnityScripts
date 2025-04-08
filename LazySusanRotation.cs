using UnityEngine;

public class LazySusanRotation : MonoBehaviour
{
    [SerializeField] private bool invertRotation = false;
    
    private bool isDragging = false;
    private Camera mainCamera;
    private float lastMouseAngle;
    private float currentRotation;

    private void Start()
    {
        mainCamera = Camera.main;
        currentRotation = transform.eulerAngles.y;
    }

    private void OnMouseDown()
    {
        isDragging = true;
        lastMouseAngle = GetMouseAngle();
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;
        
        float currentMouseAngle = GetMouseAngle();
        float deltaAngle = Mathf.DeltaAngle(lastMouseAngle, currentMouseAngle);
        
        if (invertRotation)
            deltaAngle = -deltaAngle;
            
        currentRotation += deltaAngle;
        transform.rotation = Quaternion.Euler(0f, currentRotation, 0f);
        
        lastMouseAngle = currentMouseAngle;
    }

    private float GetMouseAngle()
    {
        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            Vector3 direction = worldPos - transform.position;
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
        
        return 0f;
    }

    public bool IsBeingDragged()
    {
        return isDragging;
    }

    public float GetCurrentRotation()
    {
        return currentRotation;
    }
}