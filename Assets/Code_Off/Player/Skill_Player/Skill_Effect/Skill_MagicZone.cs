using UnityEngine;

public class Skill_MagicZone : CardEffect
{
    public GameObject magicZonePrefab; // Kéo Prefab vòng tròn phép vào đây

    public override void Activate(GameObject player)
    {
        if (magicZonePrefab != null)
        {
            // Triệu hồi phía trước mặt 5 mét, sát mặt đất
            Vector3 spawnPos = player.transform.position + player.transform.forward * 5f;
            spawnPos.y = 0.1f;

            Instantiate(magicZonePrefab, spawnPos, Quaternion.identity);

            Debug.Log("🔮 Đã tạo vùng phép!");
            DestroyCard();
        }
    }
}