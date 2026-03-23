using UnityEngine;

/// <summary>
/// Quản lý Built-in Fog — sương mù thay đổi theo ngày/đêm.
/// Gắn vào Empty GameObject. DayNightCycle gọi UpdateFog(t) mỗi frame.
/// </summary>
public class FogManager : MonoBehaviour
{
    public static FogManager instance;

    [Header("═══ FOG CƠ BẢN ═══")]
    public bool enableFog = true;
    public FogMode fogMode = FogMode.ExponentialSquared;

    [Header("--- Ngày (t=0) ---")]
    public Color dayFogColor = new Color(0.7f, 0.65f, 0.55f);
    [Tooltip("Mật độ fog (ExponentialSquared). Càng cao fog càng dày")]
    public float dayFogDensity = 0.02f;
    [Tooltip("Dùng cho Linear mode — khoảng cách bắt đầu fog")]
    public float dayFogStart = 50f;
    [Tooltip("Dùng cho Linear mode — khoảng cách fog dày nhất")]
    public float dayFogEnd = 200f;

    [Header("--- Đêm (t=1) ---")]
    public Color nightFogColor = new Color(0.03f, 0.03f, 0.06f);
    public float nightFogDensity = 0.04f;
    public float nightFogStart = 20f;
    public float nightFogEnd = 100f;

    [Header("═══ BOSS PHASE ═══")]
    public Color bossFogColor = new Color(0.08f, 0.02f, 0.02f);
    public float bossFogDensity = 0.025f;
    public bool isBossPhase = false;

    private float currentT = 0f;
    private float bossLerp = 0f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        RenderSettings.fog = enableFog;
        RenderSettings.fogMode = fogMode;
        UpdateFog(0f);
        Debug.Log($"[FogManager] Built-in Fog initialized | fog={RenderSettings.fog} mode={RenderSettings.fogMode} density={RenderSettings.fogDensity} color={RenderSettings.fogColor}");
    }

    void Update()
    {
        float targetBoss = isBossPhase ? 1f : 0f;
        bossLerp = Mathf.MoveTowards(bossLerp, targetBoss, Time.deltaTime * 0.3f);
        ApplyFogSettings(currentT);
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
        if (!enableFog) { RenderSettings.fog = false; return; }

        RenderSettings.fog = true;
        RenderSettings.fogMode = fogMode;

        // Lerp ngày → đêm
        Color baseColor = Color.Lerp(dayFogColor, nightFogColor, t);
        float baseDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, t);
        float baseStart = Mathf.Lerp(dayFogStart, nightFogStart, t);
        float baseEnd = Mathf.Lerp(dayFogEnd, nightFogEnd, t);

        // Boss phase lerp
        if (bossLerp > 0.01f)
        {
            baseColor = Color.Lerp(baseColor, bossFogColor, bossLerp);
            baseDensity = Mathf.Lerp(baseDensity, bossFogDensity, bossLerp);
        }

        RenderSettings.fogColor = baseColor;
        RenderSettings.fogDensity = baseDensity;
        RenderSettings.fogStartDistance = baseStart;
        RenderSettings.fogEndDistance = baseEnd;
    }

    public void SetBossPhase(bool active)
    {
        isBossPhase = active;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
