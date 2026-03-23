using UnityEngine;

/// <summary>
/// Đèn pin: ấn F để bật/tắt.
/// Gắn vào Player, tự tạo Spot Light con của Main Camera.
/// </summary>
public class Flashlight : MonoBehaviour
{
    [Header("--- CÀI ĐẶT ĐÈN PIN ---")]
    public KeyCode toggleKey = KeyCode.F;
    public bool startOn = false;

    [Header("--- ÁNH SÁNG ---")]
    public Color lightColor = Color.white;
    [Tooltip("Cường độ sáng")]
    public float intensity = 3f;
    [Tooltip("Góc chiếu (độ)")]
    public float spotAngle = 45f;
    [Tooltip("Tầm chiếu xa (m)")]
    public float range = 30f;

    [Header("--- ÂM THANH (tùy chọn) ---")]
    public AudioClip toggleSound;

    private Light spotLight;
    private AudioSource audioSource;

    void Start()
    {
        // Tìm Main Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Flashlight] Không tìm thấy Main Camera!");
            return;
        }

        // Tạo Spot Light con của Camera
        GameObject lightObj = new GameObject("Flashlight");
        lightObj.transform.SetParent(cam.transform);
        lightObj.transform.localPosition = Vector3.zero;
        lightObj.transform.localRotation = Quaternion.identity;

        spotLight = lightObj.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = lightColor;
        spotLight.intensity = intensity;
        spotLight.spotAngle = spotAngle;
        spotLight.range = range;
        spotLight.shadows = LightShadows.Soft;

        spotLight.enabled = startOn;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (spotLight == null) return;

        if (Input.GetKeyDown(toggleKey))
        {
            spotLight.enabled = !spotLight.enabled;

            if (toggleSound != null && audioSource != null)
                audioSource.PlayOneShot(toggleSound);
        }
    }
}
