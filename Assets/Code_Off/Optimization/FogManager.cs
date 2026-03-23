using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Quản lý HDRP Volumetric Fog — sương mù thể tích đẹp, thay đổi theo ngày/đêm.
/// Gắn vào Empty GameObject. Tự tạo Volume + Fog override.
/// DayNightCycle gọi UpdateFog(t) mỗi frame.
/// </summary>
public class FogManager : MonoBehaviour
{
    public static FogManager instance;

    [Header("═══ FOG CƠ BẢN ═══")]
    [Tooltip("Bật/Tắt toàn bộ fog")]
    public bool enableFog = true;

    [Header("--- Ngày (t=0) ---")]
    public Color dayFogColor = new Color(0.6f, 0.55f, 0.45f);
    [Tooltip("Khoảng cách bắt đầu thấy fog (m)")]
    public float dayMeanFreePath = 80f;
    [Tooltip("Độ cao fog max")]
    public float dayBaseHeight = 0f;
    [Tooltip("Độ dày fog theo chiều cao")]
    public float dayMaxHeight = 30f;

    [Header("--- Đêm (t=1) ---")]
    public Color nightFogColor = new Color(0.03f, 0.03f, 0.06f);
    public float nightMeanFreePath = 25f;
    public float nightBaseHeight = -5f;
    public float nightMaxHeight = 50f;

    [Header("═══ VOLUMETRIC FOG ═══")]
    [Tooltip("Bật sương mù thể tích (volumetric) — đẹp hơn nhưng tốn hơn")]
    public bool enableVolumetric = true;
    [Tooltip("Khoảng cách tối đa render volumetric fog")]
    public float volumetricDistance = 150f;

    [Header("--- Ngày ---")]
    [Range(0f, 1f)]
    public float dayVolumetricDimmer = 0.3f;
    [Range(0f, 1f)]
    public float dayAnisotropy = 0.6f;

    [Header("--- Đêm ---")]
    [Range(0f, 1f)]
    public float nightVolumetricDimmer = 0.8f;
    [Range(0f, 1f)]
    public float nightAnisotropy = 0.75f;

    [Header("═══ BOSS PHASE ═══")]
    [Tooltip("Fog đặc biệt khi Boss xuất hiện")]
    public Color bossFogColor = new Color(0.08f, 0.02f, 0.02f);
    public float bossMeanFreePath = 18f;
    public bool isBossPhase = false;
    [Range(0f, 1f)]
    public float bossVolumetricDimmer = 1f;

    // Internal
    private Volume fogVolume;
    private VolumeProfile fogProfile;
    private Fog fogComponent;
    private float currentT = 0f;
    private float bossLerp = 0f; // 0 = normal, 1 = boss fog

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        CreateFogVolume();
        UpdateFog(0f);
    }

    void Update()
    {
        // Lerp boss fog transition
        float targetBoss = isBossPhase ? 1f : 0f;
        bossLerp = Mathf.MoveTowards(bossLerp, targetBoss, Time.deltaTime * 0.3f);

        // Re-apply fog with boss lerp
        ApplyFogSettings(currentT);
    }

    void CreateFogVolume()
    {
        // Tạo GameObject con chứa Volume
        GameObject volObj = new GameObject("HDRP_FogVolume");
        volObj.transform.SetParent(transform);

        fogVolume = volObj.AddComponent<Volume>();
        fogVolume.isGlobal = true;
        fogVolume.priority = 10; // Cao hơn default volume

        fogProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        fogVolume.profile = fogProfile;

        // Thêm Fog override
        fogComponent = fogProfile.Add<Fog>();

        // Enable tất cả các parameter
        fogComponent.enabled.Override(true);
        fogComponent.meanFreePath.Override(dayMeanFreePath);
        fogComponent.baseHeight.Override(dayBaseHeight);
        fogComponent.maximumHeight.Override(dayMaxHeight);
        fogComponent.albedo.Override(dayFogColor);

        // Volumetric settings
        fogComponent.enableVolumetricFog.Override(enableVolumetric);
        if (enableVolumetric)
        {
            fogComponent.anisotropy.Override(dayAnisotropy);
        }

        Debug.Log("[FogManager] HDRP Volumetric Fog initialized");
    }

    /// <summary>
    /// Gọi từ DayNightCycle mỗi frame. t: 0=ngày, 1=đêm
    /// </summary>
    public void UpdateFog(float t)
    {
        currentT = t;
        ApplyFogSettings(t);
    }

    void ApplyFogSettings(float t)
    {
        if (fogComponent == null || !enableFog) return;

        fogComponent.enabled.Override(true);

        // Lerp giữa ngày và đêm
        Color baseColor = Color.Lerp(dayFogColor, nightFogColor, t);
        float baseMFP = Mathf.Lerp(dayMeanFreePath, nightMeanFreePath, t);
        float baseHeight = Mathf.Lerp(dayBaseHeight, nightBaseHeight, t);
        float maxHeight = Mathf.Lerp(dayMaxHeight, nightMaxHeight, t);
        float volDimmer = Mathf.Lerp(dayVolumetricDimmer, nightVolumetricDimmer, t);
        float aniso = Mathf.Lerp(dayAnisotropy, nightAnisotropy, t);

        // Nếu boss → lerp thêm về boss fog
        if (bossLerp > 0.01f)
        {
            baseColor = Color.Lerp(baseColor, bossFogColor, bossLerp);
            baseMFP = Mathf.Lerp(baseMFP, bossMeanFreePath, bossLerp);
            volDimmer = Mathf.Lerp(volDimmer, bossVolumetricDimmer, bossLerp);
        }

        fogComponent.albedo.Override(baseColor);
        fogComponent.meanFreePath.Override(baseMFP);
        fogComponent.baseHeight.Override(baseHeight);
        fogComponent.maximumHeight.Override(maxHeight);

        fogComponent.enableVolumetricFog.Override(enableVolumetric);
        if (enableVolumetric)
        {
            fogComponent.anisotropy.Override(aniso);
        }
    }

    /// <summary>
    /// Gọi khi Boss spawn — fog chuyển đỏ tối
    /// </summary>
    public void SetBossPhase(bool active)
    {
        isBossPhase = active;
    }

    void OnDestroy()
    {
        if (fogProfile != null)
            DestroyImmediate(fogProfile);
        if (instance == this)
            instance = null;
    }
}
