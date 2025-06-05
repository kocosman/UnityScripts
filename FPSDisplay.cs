using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F;
    public bool showFPS = true;
    public Color textColor = Color.white;
    public int fontSize = 18;

    private float deltaTime = 0.0f;
    private GUIStyle style;

    private void Update()
    {
        // Toggle visibility
        if (Input.GetKeyDown(toggleKey))
        {
            showFPS = !showFPS;
        }

        // Update deltaTime for FPS calculation
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        if (!showFPS) return;

        if (style == null)
        {
            style = new GUIStyle();
            style.fontSize = fontSize;
            style.normal.textColor = textColor;
        }

        float fps = 1.0f / deltaTime;
        string text = $"FPS: {Mathf.Ceil(fps)}";

        GUI.Label(new Rect(10, 10, 150, 30), text, style);
    }
}
