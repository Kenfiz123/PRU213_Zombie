using UnityEngine;
using TMPro;

/// <summary>
/// Số damage bay lên từ điểm trúng, luôn quay về camera.
/// Trắng = thường, Đỏ + to = headshot.
/// Được spawn bởi DamageNumberManager, tự trả về pool.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    private TextMeshPro tmp;
    private float lifetime;
    private float elapsed;
    private Vector3 velocity;
    private Color baseColor;
    private float baseScale;
    private Camera cam;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp == null)
            tmp = gameObject.AddComponent<TextMeshPro>();

        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 100;
        tmp.enableWordWrapping = false;
    }

    public void Init(float damage, bool isHeadshot, float duration = 1.2f)
    {
        elapsed = 0f;
        lifetime = duration;
        cam = Camera.main;

        // Nội dung
        if (isHeadshot)
        {
            tmp.text = Mathf.RoundToInt(damage).ToString() + "!";
            baseColor = new Color(1f, 0.15f, 0.1f); // Đỏ
            baseScale = 0.6f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(80, 0, 0, 255);
        }
        else
        {
            tmp.text = Mathf.RoundToInt(damage).ToString();
            baseColor = new Color(1f, 0.85f, 0.1f); // Vàng
            baseScale = 0.4f;
            tmp.fontStyle = FontStyles.Normal;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);
        }

        tmp.color = baseColor;
        tmp.fontSize = 6;
        transform.localScale = Vector3.one * baseScale;

        // Bay lên + random trái/phải nhẹ
        float randomX = Random.Range(-0.3f, 0.3f);
        float randomZ = Random.Range(-0.3f, 0.3f);
        velocity = new Vector3(randomX, 1.5f, randomZ);

        // Headshot: punch scale lên
        if (isHeadshot)
            transform.localScale = Vector3.one * baseScale * 1.5f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            ReturnToPool();
            return;
        }

        float t = elapsed / lifetime;

        // Bay lên, chậm dần
        transform.position += velocity * Time.deltaTime;
        velocity.y -= Time.deltaTime * 0.8f; // Gravity nhẹ

        // Scale: headshot punch down về bình thường, normal giữ nguyên
        float scale = baseScale;
        if (t < 0.15f)
            scale = Mathf.Lerp(baseScale * 1.5f, baseScale, t / 0.15f);
        transform.localScale = Vector3.one * scale;

        // Fade out nửa sau
        if (t > 0.5f)
        {
            float alpha = Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
            Color c = baseColor;
            c.a = alpha;
            tmp.color = c;
        }

        // Billboard — luôn quay về camera
        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }
    }

    void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.ReturnToPool(gameObject, 0f);
        else
            Destroy(gameObject);
    }
}
