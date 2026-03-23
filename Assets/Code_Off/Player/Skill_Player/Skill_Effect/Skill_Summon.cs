using UnityEngine;

public class Skill_Summon : CardEffect
{
    public GameObject allyPrefab; // Kéo Prefab đệ tử vào đây

    public override void Activate(GameObject player)
    {
        if (allyPrefab != null)
        {
            // Triệu hồi cách người chơi 2 mét bên phải
            Vector3 spawnPos = player.transform.position + player.transform.right * 2f;
            Instantiate(allyPrefab, spawnPos, Quaternion.identity);

            Debug.Log("🤖 Đã triệu hồi đồng minh!");
            DestroyCard();
        }
    }
}