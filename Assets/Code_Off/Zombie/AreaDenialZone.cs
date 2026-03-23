using UnityEngine;

// Gắn script này vào prefab vùng nguy hiểm (areaDenialPrefab) nếu bạn muốn nó tự gây damage lên Player.
// Damage có thể set trực tiếp trên prefab (Inspector) hoặc được Boss set sau khi Instantiate.
public class AreaDenialZone : MonoBehaviour
{
    [Header("Damage")]
    public float damagePerSecond = 10f;

    [Header("Lifetime")]
    public float lifeTime = 6f;

    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);
    }

    // Dùng trigger để gây damage theo thời gian
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerHealth pHealth = other.GetComponent<PlayerHealth>();
        if (pHealth == null) pHealth = other.GetComponentInParent<PlayerHealth>();
        if (pHealth == null) return;

        pHealth.TakeDamage(damagePerSecond * Time.deltaTime);
    }
}

