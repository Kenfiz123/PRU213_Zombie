using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair + Hit Marker giữa màn hình.
/// Gắn vào Canvas. Tự tạo UI bằng code.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    public static CrosshairUI Instance { get; private set; }

    [Header("--- CROSSHAIR ---")]
    public Color crosshairColor = Color.white;
    public int crosshairSize = 2;
    public int crosshairLength = 10;
    public int crosshairGap = 4;

    [Header("--- HIT MARKER ---")]
    public Color hitColor = new Color(1f, 0.3f, 0.3f, 1f); // Đỏ khi trúng enemy
    public Color headshotHitColor = new Color(1f, 0f, 0f, 1f); // Đỏ đậm khi headshot
    public float hitMarkerDuration = 0.15f;

    private Image[] crosshairLines = new Image[4]; // Top, Bottom, Left, Right
    private Image[] hitMarkerLines = new Image[4];
    private float hitTimer = 0f;
    private bool isHeadshot = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        CreateCrosshair();
        CreateHitMarker();
    }

    void CreateCrosshair()
    {
        // 4 đường kẻ: trên, dưới, trái, phải
        Vector2[] positions = {
            new Vector2(0, crosshairGap + crosshairLength / 2f),   // Top
            new Vector2(0, -(crosshairGap + crosshairLength / 2f)),// Bottom
            new Vector2(-(crosshairGap + crosshairLength / 2f), 0),// Left
            new Vector2(crosshairGap + crosshairLength / 2f, 0)    // Right
        };
        Vector2[] sizes = {
            new Vector2(crosshairSize, crosshairLength), // Top (dọc)
            new Vector2(crosshairSize, crosshairLength), // Bottom (dọc)
            new Vector2(crosshairLength, crosshairSize), // Left (ngang)
            new Vector2(crosshairLength, crosshairSize)  // Right (ngang)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject obj = new GameObject($"Crosshair_{i}");
            obj.transform.SetParent(transform, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = positions[i];
            rt.sizeDelta = sizes[i];

            Image img = obj.AddComponent<Image>();
            img.color = crosshairColor;
            img.raycastTarget = false;

            crosshairLines[i] = img;
        }

        // Chấm giữa nhỏ
        GameObject dot = new GameObject("Crosshair_Dot");
        dot.transform.SetParent(transform, false);
        RectTransform dotRt = dot.AddComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0.5f, 0.5f);
        dotRt.anchorMax = new Vector2(0.5f, 0.5f);
        dotRt.sizeDelta = new Vector2(2, 2);
        Image dotImg = dot.AddComponent<Image>();
        dotImg.color = crosshairColor;
        dotImg.raycastTarget = false;
    }

    void CreateHitMarker()
    {
        // 4 đường chéo X khi trúng
        float offset = 6f;
        float len = 8f;
        float[] angles = { 45f, 135f, 225f, 315f };
        Vector2[] offsets = {
            new Vector2(offset, offset),
            new Vector2(-offset, offset),
            new Vector2(-offset, -offset),
            new Vector2(offset, -offset)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject obj = new GameObject($"HitMarker_{i}");
            obj.transform.SetParent(transform, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offsets[i];
            rt.sizeDelta = new Vector2(2, len);
            rt.localRotation = Quaternion.Euler(0, 0, angles[i]);

            Image img = obj.AddComponent<Image>();
            img.color = hitColor;
            img.raycastTarget = false;

            hitMarkerLines[i] = img;
            obj.SetActive(false);
        }
    }

    void Update()
    {
        if (hitTimer > 0)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0)
            {
                for (int i = 0; i < 4; i++)
                    hitMarkerLines[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Gọi khi bắn trúng enemy</summary>
    public void ShowHitMarker(bool headshot = false)
    {
        isHeadshot = headshot;
        Color c = headshot ? headshotHitColor : hitColor;

        for (int i = 0; i < 4; i++)
        {
            hitMarkerLines[i].color = c;
            hitMarkerLines[i].gameObject.SetActive(true);
        }

        hitTimer = hitMarkerDuration;
        if (headshot) hitTimer *= 2f; // Headshot hiện lâu hơn
    }
}
