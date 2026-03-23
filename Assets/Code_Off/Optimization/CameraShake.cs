using UnityEngine;
using System.Collections;

/// <summary>
/// Camera Shake - Rung camera khi nổ, bị đánh, boss stomp.
/// Gắn vào Camera chính (hoặc parent của Camera).
/// Gọi: CameraShake.Shake(duration, magnitude)
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("--- CÀI ĐẶT MẶC ĐỊNH ---")]
    [Tooltip("Cường độ rung mặc định")]
    public float defaultMagnitude = 0.15f;
    [Tooltip("Thời gian rung mặc định (giây)")]
    public float defaultDuration = 0.3f;

    private Coroutine shakeRoutine;
    private Vector3 originalLocalPos;

    void Awake()
    {
        Instance = this;
        originalLocalPos = transform.localPosition;
    }

    // ═══════════════════════════════════════════════════════════════
    // STATIC API - gọi từ bất kỳ đâu
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Rung camera với duration và magnitude tùy chỉnh</summary>
    public static void Shake(float duration, float magnitude)
    {
        if (Instance != null)
            Instance.DoShake(duration, magnitude);
    }

    /// <summary>Rung camera mặc định</summary>
    public static void Shake()
    {
        if (Instance != null)
            Instance.DoShake(Instance.defaultDuration, Instance.defaultMagnitude);
    }

    // === PRESET: Nổ (barrel, grenade) ===
    public static void ShakeExplosion()
    {
        Shake(0.4f, 0.3f);
    }

    // === PRESET: Bị đánh thường ===
    public static void ShakeHit()
    {
        Shake(0.15f, 0.1f);
    }

    // === PRESET: Boss đánh mạnh ===
    public static void ShakeBossHit()
    {
        Shake(0.5f, 0.4f);
    }

    // === PRESET: Bắn súng (nhẹ) ===
    public static void ShakeShoot()
    {
        Shake(0.05f, 0.02f);
    }

    // ═══════════════════════════════════════════════════════════════
    // NỘI BỘ
    // ═══════════════════════════════════════════════════════════════

    void DoShake(float duration, float magnitude)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Giảm dần cường độ theo thời gian
            float progress = elapsed / duration;
            float currentMag = magnitude * (1f - progress);

            float offsetX = Random.Range(-1f, 1f) * currentMag;
            float offsetY = Random.Range(-1f, 1f) * currentMag;

            transform.localPosition = originalLocalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        shakeRoutine = null;
    }

    void OnDisable()
    {
        // Reset vị trí khi tắt
        transform.localPosition = originalLocalPos;
    }
}
