using UnityEngine;
using TMPro; // Dùng để hiện chữ "Press E"

public class ItemPickup : MonoBehaviour
{
    public enum ItemType { Medkit, Armor, PistolAmmo, AKAmmo }

    [Header("--- CÀI ĐẶT VẬT PHẨM ---")]
    public ItemType itemType;   // Chọn loại vật phẩm
    public int amount = 1;      // Số lượng (ví dụ đạn thì là 30)

    [Header("--- UI NHẮC NHỞ ---")]
    public GameObject pickupText; // Kéo cái Text "Press E" vào đây

    private bool isPlayerNearby = false;
    private PlayerInventory playerInventory; // Tham chiếu đến balo của Player

    void Start()
    {
        // Ẩn chữ Press E lúc đầu
        if (pickupText != null) pickupText.SetActive(false);
    }

    void Update()
    {
        if (!isPlayerNearby)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null && Vector3.Distance(transform.position, player.transform.position) < 2.5f)
            {
                playerInventory = player.GetComponent<PlayerInventory>();
                if (playerInventory != null)
                {
                    isPlayerNearby = true;
                    InteractionManager.Register(GetInstanceID());
                    if (pickupText != null) pickupText.SetActive(true);
                }
            }
        }

        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            CollectItem();
        }
    }

    void CollectItem()
    {
        if (playerInventory != null)
        {
            string typeString = "";
            switch (itemType)
            {
                case ItemType.Medkit: typeString = "Medkit"; break;
                case ItemType.Armor: typeString = "Armor"; break;
                case ItemType.PistolAmmo: typeString = "PistolAmmo"; break;
                case ItemType.AKAmmo: typeString = "AKAmmo"; break;
            }

            playerInventory.AddItem(typeString, amount);
            Debug.Log($"Đã nhặt {amount} {typeString}");
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        DetectPlayer(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (!isPlayerNearby) DetectPlayer(other);
    }

    void DetectPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            InteractionManager.Register(GetInstanceID());
            if (pickupText != null) pickupText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerInventory = null;
            InteractionManager.Unregister(GetInstanceID());
            if (pickupText != null) pickupText.SetActive(false);
        }
    }

    void OnDestroy()
    {
        InteractionManager.Unregister(GetInstanceID());
    }
}