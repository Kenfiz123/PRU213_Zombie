using UnityEngine;

/// <summary>
/// Hiện FPS góc trên trái màn hình để theo dõi hiệu năng.
/// Gắn vào Main Camera hoặc Empty GameObject.
/// </summary>
public class FPSCounter : MonoBehaviour
{
    [Header("--- HIỂN THỊ ---")]
    public bool showFPS = true;
    public int fontSize = 22;
    public Color goodColor = Color.green;    // >= 50 FPS
    public Color okColor = Color.yellow;     // >= 30 FPS
    public Color badColor = Color.red;       // < 30 FPS

    private float deltaTime = 0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (!showFPS) return;

        float fps = 1.0f / deltaTime;

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = fontSize;

        if (fps >= 50) style.normal.textColor = goodColor;
        else if (fps >= 30) style.normal.textColor = okColor;
        else style.normal.textColor = badColor;

        string text = $"FPS: {fps:F0}";
        Rect rect = new Rect(10, 10, 200, 40);

        // Shadow
        GUIStyle shadow = new GUIStyle(style);
        shadow.normal.textColor = Color.black;
        GUI.Label(new Rect(12, 12, 200, 40), text, shadow);
        GUI.Label(rect, text, style);
    }
}
