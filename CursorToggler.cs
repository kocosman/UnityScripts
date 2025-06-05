using UnityEngine;

public class CursorToggler : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The key that toggles cursor visibility")]
    [SerializeField] private KeyCode toggleKey = KeyCode.C;
    
    [Tooltip("Lock cursor position when hidden")]
    [SerializeField] private bool lockCursorWhenHidden = true;
    
    [Header("Status")]
    [SerializeField] private bool cursorVisible = true;
    
    void Start()
    {
        // Set initial cursor state
        UpdateCursorState();
    }
    
    void Update()
    {
        // Check for key press
        if (Input.GetKeyDown(toggleKey))
        {
            // Toggle cursor visibility
            cursorVisible = !cursorVisible;
            UpdateCursorState();
        }
        
        // Alternative way to show cursor in case it gets stuck (press Escape)
        if (Input.GetKeyDown(KeyCode.Escape) && !cursorVisible)
        {
            cursorVisible = true;
            UpdateCursorState();
        }
    }
    
    private void UpdateCursorState()
    {
        // Set cursor visibility
        Cursor.visible = cursorVisible;
        
        // Set cursor lock state if configured
        if (lockCursorWhenHidden)
        {
            Cursor.lockState = cursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
        }
        
        Debug.Log("Cursor is now " + (cursorVisible ? "visible" : "hidden"));
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // Ensure cursor state is maintained when application focus changes
        if (hasFocus)
        {
            UpdateCursorState();
        }
    }
}