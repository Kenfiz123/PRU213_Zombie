using UnityEngine;
using System.Collections;

public class Skill_PowerBuff : CardEffect
{
    public float duration = 10f; // Thời gian buff
    public float multiplier = 2f; // Gấp đôi sức mạnh

    public override void Activate(GameObject player)
    {
        // Tìm súng trên người Player
        PlayerShooting gun = player.GetComponentInChildren<PlayerShooting>();

        if (gun != null)
        {
            // Thêm một component tạm thời vào Player để đếm giờ
            // (Vì thẻ bài sẽ bị hủy ngay nên không thể đếm giờ trên thẻ được)
            BuffHandler buff = player.gameObject.AddComponent<BuffHandler>();
            buff.StartBuff(gun, duration, multiplier);

            Debug.Log("💪 Sức mạnh trỗi dậy!");
            DestroyCard();
        }
    }
}

// Class phụ để xử lý việc đếm giờ (Nằm chung trong file này cũng được)
public class BuffHandler : MonoBehaviour
{
    public void StartBuff(PlayerShooting gun, float time, float mul)
    {
        StartCoroutine(BuffRoutine(gun, time, mul));
    }

    IEnumerator BuffRoutine(PlayerShooting gun, float time, float mul)
    {
        // 1. Tăng chỉ số
        gun.damageMultiplier = mul;
        gun.reloadSpeedMultiplier = mul;

        // 2. Chờ
        yield return new WaitForSeconds(time);

        // 3. Trả về cũ
        gun.damageMultiplier = 1f;
        gun.reloadSpeedMultiplier = 1f;

        Debug.Log("zzz Hết Buff Sức Mạnh");

        // 4. Tự hủy script đếm giờ này đi cho nhẹ người Player
        Destroy(this);
    }
}