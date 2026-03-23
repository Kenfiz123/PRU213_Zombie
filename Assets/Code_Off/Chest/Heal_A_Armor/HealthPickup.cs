using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("--- CÀI ĐẶT ---")]
    [Tooltip("Lượng máu sẽ hồi phục")]
    public float healAmount = 50f;

    [Header("--- INVENTORY ---")]
    [Tooltip("Số Medkit sẽ cộng vào balo (Inventory). Để 0 nếu không muốn cộng.")]
    public int medkitAmount = 1;

    [Header("--- HIỆU ỨNG (Tùy chọn) ---")]
    public AudioClip pickupSound;
    public GameObject pickupEffect;

    private PlayerInventory playerInventory;
    private bool playerInRange = false;

    void Update()
    {
        // Fallback: nếu trigger không phát hiện, dùng distance check
        if (!playerInRange)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null && Vector3.Distance(transform.position, player.transform.position) < 2.5f)
            {
                playerInventory = player.GetComponent<PlayerInventory>();
                if (playerInventory != null)
                {
                    playerInRange = true;
                    InteractionManager.Register(GetInstanceID());
                    Debug.Log("[HealthPickup] Phát hiện Player bằng distance check");
                }
            }
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[HealthPickup] E pressed, nhặt item!");
            TryPickup();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        DetectPlayer(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (!playerInRange) DetectPlayer(other);
    }

    void DetectPlayer(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInRange = true;
            InteractionManager.Register(GetInstanceID());
            Debug.Log("[HealthPickup] Phát hiện Player bằng trigger");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        playerInventory = null;
        InteractionManager.Unregister(GetInstanceID());
    }

    void OnDestroy()
    {
        InteractionManager.Unregister(GetInstanceID());
    }

    void TryPickup()
    {
        if (playerInventory == null) return;

        if (medkitAmount > 0)
        {
            playerInventory.AddItem("Medkit", medkitAmount);
        }

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Debug.Log($"Nhặt Heal: +{medkitAmount} Medkit.");
        Destroy(gameObject);
    }
}
