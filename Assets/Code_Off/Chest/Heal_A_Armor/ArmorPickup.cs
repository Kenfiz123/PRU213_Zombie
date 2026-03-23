using UnityEngine;

public class ArmorPickup : MonoBehaviour
{
    [Header("--- CÀI ĐẶT ---")]
    [Tooltip("Lượng giáp sẽ hồi phục")]
    public float armorAmount = 50f;

    [Header("--- INVENTORY ---")]
    [Tooltip("Số Armor Plate sẽ cộng vào balo (Inventory). Để 0 nếu không muốn cộng.")]
    public int armorPlateAmount = 1;

    [Header("--- HIỆU ỨNG ---")]
    public AudioClip pickupSound;
    public GameObject pickupEffect;

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

        if (armorPlateAmount > 0)
        {
            playerInventory.AddItem("Armor", armorPlateAmount);
        }

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Debug.Log($"Nhặt Armor: +{armorPlateAmount} Armor Plate.");
        Destroy(gameObject);
    }
}
