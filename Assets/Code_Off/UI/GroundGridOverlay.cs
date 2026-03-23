using UnityEngine;

/// <summary>
/// Tạo texture lưới cho mặt đường, giúp dễ nhìn và xác định di chuyển.
/// Gắn vào GameObject mặt đường (Plane/Terrain).
/// </summary>
public class GroundGridOverlay : MonoBehaviour
{
    [Header("--- LƯỚI ---")]
    [Tooltip("Kích thước texture (pixel)")]
    public int textureSize = 512;
    [Tooltip("Số ô lưới mỗi chiều")]
    public int gridCells = 16;
    [Tooltip("Độ dày đường kẻ (pixel)")]
    public int lineThickness = 2;

    [Header("--- MÀU SẮC ---")]
    public Color groundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color gridLineColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color accentLineColor = new Color(0.5f, 0.45f, 0.3f, 1f);
    [Tooltip("Mỗi bao nhiêu ô thì có đường đậm hơn")]
    public int accentEvery = 4;

    [Header("--- TILING ---")]
    [Tooltip("Số lần lặp texture trên bề mặt")]
    public float tiling = 4f;

    void Start()
    {
        ApplyGridTexture();
    }

    public void ApplyGridTexture()
    {
        Texture2D tex = GenerateGridTexture();

        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        // Tạo material mới để không ảnh hưởng material gốc
        Material mat = new Material(rend.material);
        mat.mainTexture = tex;
        mat.mainTextureScale = new Vector2(tiling, tiling);

        // Nếu HDRP: set BaseColorMap
        if (mat.HasProperty("_BaseColorMap"))
            mat.SetTexture("_BaseColorMap", tex);

        // Set base color để không bị quá tối
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);

        rend.material = mat;
    }

    Texture2D GenerateGridTexture()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;

        int cellSize = textureSize / gridCells;

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                // Mặc định: màu nền
                Color pixel = groundColor;

                // Thêm noise nhẹ cho tự nhiên
                float noise = Random.Range(-0.03f, 0.03f);
                pixel = new Color(pixel.r + noise, pixel.g + noise, pixel.b + noise, 1f);

                // Vẽ đường kẻ lưới
                int cellX = x % cellSize;
                int cellY = y % cellSize;

                bool isLine = cellX < lineThickness || cellY < lineThickness;

                if (isLine)
                {
                    // Kiểm tra đường đậm (accent)
                    int gridX = x / cellSize;
                    int gridY = y / cellSize;
                    bool isAccent = (gridX % accentEvery == 0) || (gridY % accentEvery == 0);

                    pixel = isAccent ? accentLineColor : gridLineColor;
                }

                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();
        return tex;
    }
}
