using UnityEngine;

public class AmmoPickupPistol : MonoBehaviour
{
    [Header("--- CẤU HÌNH ĐẠN LỤC ---")]
    public int ammoAmount = 12;

    [Header("--- UI NHẮC NHỞ (TÙY CHỌN) ---")]
    public GameObject pickupText;

    private PlayerInventory playerInventory;
    private bool playerInRange = false;

    void Update()
    {
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
                    if (pickupText != null) pickupText.SetActive(true);
                }
            }
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
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
            if (pickupText != null) pickupText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        playerInventory = null;
        InteractionManager.Unregister(GetInstanceID());
        if (pickupText != null) pickupText.SetActive(false);
    }

    void OnDestroy()
    {
        InteractionManager.Unregister(GetInstanceID());
    }

    void TryPickup()
    {
        if (playerInventory == null) return;

        if (ammoAmount > 0)
        {
            playerInventory.AddItem("PistolAmmo", ammoAmount);
            Debug.Log($"Nhặt Ammo Pistol: +{ammoAmount} viên.");
        }

        if (pickupText != null) pickupText.SetActive(false);
        Destroy(gameObject);
    }
}
