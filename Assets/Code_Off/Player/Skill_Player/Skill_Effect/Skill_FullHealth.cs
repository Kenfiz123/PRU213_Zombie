using UnityEngine;

public class Skill_FullHealth : CardEffect
{
    public override void Activate(GameObject player)
    {
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(9999); // Hồi số lượng cực lớn
            Debug.Log("❤️ Đã hồi Full Máu!");
            DestroyCard(); // Tự hủy thẻ
        }
    }
}