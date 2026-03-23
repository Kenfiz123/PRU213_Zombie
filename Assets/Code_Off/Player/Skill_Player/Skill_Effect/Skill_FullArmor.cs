using UnityEngine;

public class Skill_FullArmor : CardEffect
{
    public override void Activate(GameObject player)
    {
        PlayerArmor armor = player.GetComponent<PlayerArmor>();
        if (armor != null)
        {
            armor.AddArmor(9999); // Hồi số lượng cực lớn
            Debug.Log("🛡️ Đã hồi Full Giáp!");
            DestroyCard();
        }
    }
}