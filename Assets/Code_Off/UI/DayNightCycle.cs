using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Làm bầu trời tối dần liên tục theo thời gian thực.
/// Khi chuyển phase, nếu chưa đạt mốc tối tối thiểu → nhảy tới mốc đó.
/// Nếu đã vượt mốc → cộng thêm thời gian bonus.
/// Gắn vào Directional Light.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("--- THỜI GIAN ---")]
    [Tooltip("Tổng thời gian từ sáng → tối hoàn toàn (giây)")]
    public float totalDuration = 600f;

    [Header("--- MỐC TỐI THIỂU MỖI PHASE (0-1) ---")]
    [Tooltip("Phase 1 bắt đầu ở mức sáng này")]
    public float phase1MinProgress = 0f;
    [Tooltip("Phase 2 ít nhất phải tối bằng này")]
    public float phase2MinProgress = 0.35f;
    [Tooltip("Phase 3 ít nhất phải tối bằng này")]
    public float phase3MinProgress = 0.7f;
    [Tooltip("Boss tối hoàn toàn")]
    public float bossMinProgress = 1f;

    [Header("--- BONUS KHI VƯỢT MỐC (giây) ---")]
    [Tooltip("Nếu đã vượt mốc phase, cộng thêm bao nhiêu giây tối")]
    public float bonusTimeOnPhaseChange = 30f;

    [Header("--- ÁNH SÁNG BẮT ĐẦU (Chiều) ---")]
    public Color startLightColor = new Color(1f, 0.55f, 0.2f);
    public float startIntensity = 1.5f;
    public float startRotationX = 15f;

    [Header("--- ÁNH SÁNG KẾT THÚC (Đêm) ---")]
    public Color endLightColor = new Color(0.05f, 0.05f, 0.1f);
    public float endIntensity = 0.02f;
    public float endRotationX = -30f;

    [Header("--- MÀU BẦU TRỜI ---")]
    public Color startAmbientColor = new Color(0.4f, 0.3f, 0.2f);
    public Color endAmbientColor = new Color(0.02f, 0.02f, 0.04f);

    [Header("--- SƯƠNG MÙ ---")]
    public bool enableFog = true;
    public Color startFogColor = new Color(0.5f, 0.4f, 0.3f);
    public Color endFogColor = new Color(0.02f, 0.02f, 0.03f);
    public float startFogDensity = 0.005f;
    public float endFogDensity = 0.035f;

    [Header("--- MẶT TRĂNG MÁU ---")]
    [Tooltip("Bật mặt trăng máu khi trời tối")]
    public bool enableBloodMoon = true;
    [Tooltip("Mức tối bắt đầu hiện trăng (0-1)")]
    public float moonAppearProgress = 0.3f;
    [Tooltip("Kích thước trăng")]
    public float moonSize = 60f;
    [Tooltip("Khoảng cách trăng so với camera")]
    public float moonDistance = 500f;
    public Color moonColor = new Color(0.8f, 0.1f, 0.05f, 1f);
    public Color moonGlowColor = new Color(1f, 0.2f, 0.1f, 0.3f);

    private GameObject moonObject;
    private GameObject moonGlow;
    private MeshRenderer moonRenderer;
    private MeshRenderer glowRenderer;

    private Light directionalLight;
    private float progress = 0f; // 0 = sáng, 1 = tối hoàn toàn

    void Start()
    {
        directionalLight = GetComponent<Light>();
        if (directionalLight == null)
        {
            Debug.LogWarning("[DayNight] Không tìm thấy Light component!");
            return;
        }

        if (enableBloodMoon)
            CreateBloodMoon();

        ApplyLighting(0f);
    }

    void Update()
    {
        if (directionalLight == null) return;

        // Luôn trôi theo thời gian thực, không reset
        progress += Time.deltaTime / totalDuration;
        progress = Mathf.Clamp01(progress);

        ApplyLighting(progress);
        UpdateBloodMoon(progress);
    }

    /// <summary>
    /// Gọi khi chuyển phase. phaseIndex: 0=Phase1, 1=Phase2, 2=Phase3, 3=Phase4, 4=Boss
    /// Nếu chưa đạt mốc tối thiểu → nhảy tới mốc.
    /// Nếu đã vượt mốc → cộng thêm bonus time.
    /// </summary>
    public void OnPhaseChanged(int phaseIndex)
    {
        float minProgress = GetMinProgress(phaseIndex);

        if (progress < minProgress)
        {
            // Chưa tới mốc → nhảy tới
            progress = minProgress;
        }
        else
        {
            // Đã vượt mốc → cộng thêm bonus
            float bonusProgress = bonusTimeOnPhaseChange / totalDuration;
            progress = Mathf.Clamp01(progress + bonusProgress);
        }

        ApplyLighting(progress);
    }

    float GetMinProgress(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0: return phase1MinProgress;
            case 1: return phase2MinProgress;
            case 2: return phase3MinProgress;
            default: return bossMinProgress; // Phase 3+ hoặc Boss
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // MẶT TRĂNG MÁU
    // ═══════════════════════════════════════════════════════════════
    void CreateBloodMoon()
    {
        // Tạo Quad (mặt phẳng) thay vì Sphere — nhẹ hơn, dùng texture tự tạo
        // GLOW (to, mờ)
        moonGlow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        moonGlow.name = "MoonGlow";
        Destroy(moonGlow.GetComponent<Collider>());
        glowRenderer = moonGlow.GetComponent<MeshRenderer>();
        glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        glowRenderer.receiveShadows = false;

        // MOON (nhỏ, rõ)
        moonObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        moonObject.name = "BloodMoon";
        Destroy(moonObject.GetComponent<Collider>());
        moonRenderer = moonObject.GetComponent<MeshRenderer>();
        moonRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        moonRenderer.receiveShadows = false;

        // Tạo texture tròn
        Texture2D moonTex = CreateCircleTexture(256, 1f);
        Texture2D glowTex = CreateCircleTexture(256, 0.3f);

        // Tạo material dùng built-in Unlit shader (luôn có sẵn)
        Material moonMat = CreateSimpleUnlitMaterial(moonTex, moonColor);
        Material glowMat = CreateSimpleUnlitMaterial(glowTex, moonGlowColor);

        moonRenderer.material = moonMat;
        glowRenderer.material = glowMat;

        moonObject.SetActive(false);
        moonGlow.SetActive(false);
    }

    Texture2D CreateCircleTexture(int size, float edgeSoftness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f;
        float softPixels = radius * edgeSoftness;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) / Mathf.Max(softPixels, 1f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    Material CreateSimpleUnlitMaterial(Texture2D tex, Color color)
    {
        // Particles/Standard Unlit — luôn có sẵn trong mọi render pipeline
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("UI/Default");

        Material mat = new Material(shader);
        mat.mainTexture = tex;
        mat.color = color;

        // Bật transparent
        mat.SetFloat("_Mode", 2); // Fade mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    void UpdateBloodMoon(float t)
    {
        if (!enableBloodMoon || moonObject == null) return;

        if (t < moonAppearProgress)
        {
            moonObject.SetActive(false);
            moonGlow.SetActive(false);
            return;
        }

        moonObject.SetActive(true);
        moonGlow.SetActive(true);

        float moonT = Mathf.InverseLerp(moonAppearProgress, 1f, t);

        Camera cam = Camera.main;
        if (cam == null) return;

        // Vị trí trăng trên bầu trời — cố định hướng (không di chuyển theo camera rotation)
        // Góc: phía trên bên phải, cao trên trời
        float elevation = Mathf.Lerp(25f, 55f, moonT); // Độ cao (độ)
        float azimuth = -30f; // Hướng (độ) — hơi bên trái

        float elevRad = elevation * Mathf.Deg2Rad;
        float azimRad = azimuth * Mathf.Deg2Rad;

        Vector3 moonDir = new Vector3(
            Mathf.Sin(azimRad) * Mathf.Cos(elevRad),
            Mathf.Sin(elevRad),
            Mathf.Cos(azimRad) * Mathf.Cos(elevRad)
        ).normalized;

        Vector3 moonPos = cam.transform.position + moonDir * moonDistance;
        moonObject.transform.position = moonPos;
        moonGlow.transform.position = moonPos;

        // Luôn quay mặt về camera
        moonObject.transform.LookAt(cam.transform);
        moonObject.transform.Rotate(0, 180, 0); // Quad mặc định quay ngược
        moonGlow.transform.LookAt(cam.transform);
        moonGlow.transform.Rotate(0, 180, 0);

        // Scale to dần
        float scale = Mathf.Lerp(moonSize * 0.3f, moonSize, moonT);
        moonObject.transform.localScale = Vector3.one * scale;
        moonGlow.transform.localScale = Vector3.one * scale * 3f;

        // Màu đậm dần
        if (moonRenderer != null)
        {
            Color mc = moonColor;
            mc.a = Mathf.Lerp(0.4f, 1f, moonT);
            moonRenderer.material.color = mc;
        }
        if (glowRenderer != null)
        {
            Color gc = moonGlowColor;
            gc.a = Mathf.Lerp(0.05f, 0.3f, moonT);
            glowRenderer.material.color = gc;
        }

        // Ambient tint đỏ nhẹ
        if (moonT > 0.3f)
        {
            float redTint = (moonT - 0.3f) / 0.7f * 0.15f;
            Color ambient = RenderSettings.ambientLight;
            ambient.r = Mathf.Max(ambient.r, redTint);
            RenderSettings.ambientLight = ambient;
        }
    }

    void ApplyLighting(float t)
    {
        if (directionalLight == null) return;

        directionalLight.color = Color.Lerp(startLightColor, endLightColor, t);
        directionalLight.intensity = Mathf.Lerp(startIntensity, endIntensity, t);

        Vector3 rot = directionalLight.transform.eulerAngles;
        rot.x = Mathf.Lerp(startRotationX, endRotationX, t);
        directionalLight.transform.eulerAngles = rot;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(startAmbientColor, endAmbientColor, t);

        // Legacy fog (fallback)
        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Color.Lerp(startFogColor, endFogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, endFogDensity, t);
        }

        // HDRP Volumetric Fog
        if (FogManager.instance != null)
            FogManager.instance.UpdateFog(t);
    }
}
