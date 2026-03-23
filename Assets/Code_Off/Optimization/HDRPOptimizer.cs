using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Tối ưu HDRP: tăng chất lượng đồ họa + giữ hiệu năng.
/// Gắn vào 1 Empty GameObject hoặc Main Camera.
/// </summary>
public class HDRPOptimizer : MonoBehaviour
{
    [Header("=== SHADOW ===")]
    [Tooltip("Khoảng cách shadow tối đa")]
    public float shadowDistance = 80f;
    [Tooltip("Shadow cascade (2 hoặc 4)")]
    public int shadowCascades = 2;

    [Header("=== TEXTURE ===")]
    [Tooltip("0=Full, 1=Half, 2=Quarter")]
    public int textureQuality = 0;
    [Tooltip("Anisotropic Filtering")]
    public AnisotropicFiltering anisotropicMode = AnisotropicFiltering.Enable;

    [Header("=== RENDERING ===")]
    [Tooltip("Pixel Light tối đa")]
    public int maxPixelLights = 4;
    [Tooltip("VSync (0=off tăng FPS, 1=on mượt)")]
    public int vSyncCount = 1;
    [Tooltip("Target FPS (0 = không giới hạn, chỉ khi VSync=0)")]
    public int targetFPS = 60;

    [Header("=== LOD ===")]
    [Tooltip("LOD Bias (1=mặc định, >1 = giữ chi tiết xa hơn)")]
    public float lodBias = 1.5f;
    [Tooltip("Max LOD Level (0=tất cả LOD, 1=bỏ LOD cao nhất)")]
    public int maxLODLevel = 0;

    [Header("=== CAMERA ===")]
    [Tooltip("Far clip plane")]
    public float cameraFarClip = 300f;

    void Start()
    {
        ApplyOptimizations();
    }

    public void ApplyOptimizations()
    {
        // === Shadow ===
        QualitySettings.shadowDistance = shadowDistance;
        QualitySettings.shadowCascades = shadowCascades;
        QualitySettings.shadows = ShadowQuality.All;

        // === Texture ===
        QualitySettings.globalTextureMipmapLimit = textureQuality;
        QualitySettings.anisotropicFiltering = anisotropicMode;

        // === Rendering ===
        QualitySettings.pixelLightCount = maxPixelLights;
        QualitySettings.vSyncCount = vSyncCount;
        if (vSyncCount == 0 && targetFPS > 0)
            Application.targetFrameRate = targetFPS;

        // === LOD ===
        QualitySettings.lodBias = lodBias;
        QualitySettings.maximumLODLevel = maxLODLevel;

        // === Camera ===
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.farClipPlane = cameraFarClip;
        }

        Debug.Log("[HDRPOptimizer] Đã áp dụng tối ưu đồ họa!");
    }
}
