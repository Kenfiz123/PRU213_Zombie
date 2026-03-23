using UnityEngine;

public class ZombieSight : MonoBehaviour
{
    [Header("Cài đặt Tầm nhìn")]
    public float viewRadius = 10f;      // Bán kính nhìn (nhìn xa bao nhiêu)
    [Range(0, 360)]
    public float viewAngle = 90f;       // Góc mở của mắt (rộng bao nhiêu)

    [Header("Mục tiêu & Vật cản")]
    public Transform player;            // Kéo nhân vật Player vào đây
    public LayerMask targetMask;        // Layer của Player
    public LayerMask obstacleMask;      // Layer của Tường/Vật cản

    public bool canSeePlayer;           // Biến này sẽ True nếu nhìn thấy

    private void Update()
    {
        canSeePlayer = CheckSight();

        if (canSeePlayer)
        {
            Debug.Log("Zombie: Tao thấy mày rồi!");
            // Sau này sẽ gọi hàm đuổi theo ở đây
        }
    }

    private bool CheckSight()
    {
        if (player == null) return false;

        // BƯỚC 1: Kiểm tra khoảng cách
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > viewRadius)
            return false; // Quá xa -> Không thấy

        // BƯỚC 2: Kiểm tra góc nhìn (Player có ở trước mặt không?)
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        // Nếu góc giữa hướng mặt zombie và hướng tới player nhỏ hơn 1 nửa góc nhìn -> Nằm trong tầm mắt
        if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2)
        {
            // BƯỚC 3: Kiểm tra vật cản (Bắn tia Raycast)
            // Bắn tia từ vị trí mắt (cộng thêm Vector3.up để cao bằng tầm mắt) đến player
            float distToTarget = Vector3.Distance(transform.position, player.position);

            // Nếu Raycast KHÔNG trúng vật cản (obstacleMask) -> Nhìn thấy!
            if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distToTarget, obstacleMask))
            {
                return true;
            }
        }

        return false;
    }

    // --- PHẦN VẼ HÌNH HỖ TRỢ (GIZMOS) ---
    // Giúp bạn nhìn thấy vùng nhìn của Zombie trong cửa sổ Scene
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Vẽ vòng tròn bán kính
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // Vẽ 2 đường giới hạn góc nhìn
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

        if (canSeePlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position); // Vẽ đường đỏ khi nhìn thấy
        }
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}