using UnityEngine;

public class CardTrigger : MonoBehaviour
{
    [Header("--- LOẠI SKILL CỦA THẺ NÀY ---")]
    public SkillType skillType = SkillType.FullHealth;

    [Header("--- NHẶT ---")]
    public float pickupRange = 3f;

    private bool picked = false;
    private bool playerInRange = false;
    private Transform playerTransform;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    void Update()
    {
        if (picked) return;

        // Fallback: check khoảng cách nếu OnTrigger không fire
        if (!playerInRange)
        {
            if (playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform != null &&
                Vector3.Distance(transform.position, playerTransform.position) <= pickupRange)
            {
                playerInRange = true;
            }
        }

        if (!playerInRange) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (CardHotbarManager.Instance == null)
        {
            Debug.LogWarning("[CardTrigger] Không tìm thấy CardHotbarManager!");
            return;
        }

        bool added = CardHotbarManager.Instance.AddCard(skillType);

        if (added)
        {
            picked = true;
            if (skillType == SkillType.Summon)
                LootChest.ResetSummonCardFlag();
            Destroy(gameObject);
        }
    }
}
