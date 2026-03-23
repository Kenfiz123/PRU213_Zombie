using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Để dùng UI Text

public class PlayerShooting : MonoBehaviour
{
    [Header("=== CẤU HÌNH SÚNG ===")]
    public float damage = 10f;          // Sát thương mỗi viên
    public float range = 100f;          // Tầm bắn
    public float fireRate = 15f;        // Tốc độ bắn
    public bool isAutomatic = true;     // True = Súng trường, False = Súng lục

    [Header("=== ĐẠN & NẠP ĐẠN ===")]
    public int maxAmmo = 30;
    private int currentAmmo;
    public float reloadTime = 1.5f;
    private bool isReloading = false;
    public Text ammoText;               // Kéo UI Text vào đây

    [Header("=== CAMERA & AIM ===")]
    public Camera fpsCam;               // Kéo Main Camera vào đây (BẮT BUỘC)
    public LayerMask shootingMask;      // Chọn "Everything"

    [Header("=== HIỆU ỨNG (VFX) ===")]
    public Transform muzzlePoint;       // Vị trí đầu nòng súng
    public ParticleSystem muzzleFlash;  // Hiệu ứng chớp lửa đầu nòng
    public GameObject impactEffect;     // Hiệu ứng bắn vào TƯỜNG (Tia lửa)
    public GameObject bloodEffect;      // Hiệu ứng bắn vào ZOMBIE (Máu)

    [Header("=== ÂM THANH (AUDIO) ===")]
    public AudioSource gunAudioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;        // Tiếng tạch tạch khi hết đạn

    [Header("=== ANIMATION ===")]
    public Animator gunAnimator;

    [Header("=== KHO ĐẠN (INVENTORY) ===")]
    [Tooltip("Tham chiếu tới PlayerInventory trên nhân vật (chứa kho đạn dự trữ).")]
    [SerializeField] private PlayerInventory inventory;
    [Tooltip("Kiểu đạn mà súng này sử dụng. Ví dụ: \"PistolAmmo\" hoặc \"AKAmmo\".")]
    [SerializeField] private string ammoType = "PistolAmmo";

    [Header("=== BUFF MULTIPLIERS (Cho Skill System) ===")]
    [Tooltip("Nhân với damage gốc (mặc định 1.0).")]
    public float damageMultiplier = 1f;
    [Tooltip("Nhân với reloadTime gốc để tăng tốc độ nạp đạn (mặc định 1.0, >1 = nạp nhanh hơn).")]
    public float reloadSpeedMultiplier = 1f;

    [Header("=== HEADSHOT ===")]
    [Tooltip("Hệ số nhân damage khi headshot")]
    public float headshotMultiplier = 2f;
    [Tooltip("Âm thanh headshot (tùy chọn)")]
    public AudioClip headshotSound;

    private float nextTimeToFire = 0f;
    private static float inputBlockUntil = 0f;

    /// <summary>Gọi khi đóng UI panel để chặn bắn trong 0.2s</summary>
    public static void BlockInputBriefly()
    {
        inputBlockUntil = Time.unscaledTime + 0.2f;
    }

    void Start()
    {
        if (currentAmmo == -1) currentAmmo = maxAmmo; // Khởi tạo đạn
        currentAmmo = maxAmmo;
        UpdateAmmoUI();

        // Tự tìm Inventory nếu chưa gán (tìm trên cha gần nhất)
        if (inventory == null)
        {
            inventory = GetComponentInParent<PlayerInventory>();
        }
    }

    void OnEnable()
    {
        isReloading = false;
        if (gunAnimator != null) gunAnimator.SetBool("Reloading", false);
    }

    void Update()
    {
        if (isReloading) return;

        // Chặn bắn khi UI đang mở hoặc vừa đóng
        if (Time.timeScale == 0f) return;
        if (Time.unscaledTime < inputBlockUntil) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // Bấm R để nạp đạn
        if (currentAmmo < maxAmmo && Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
            return;
        }

        // Hết đạn
        if (currentAmmo <= 0)
        {
            if (Input.GetButtonDown("Fire1") && emptySound != null)
            {
                gunAudioSource.PlayOneShot(emptySound);
            }
            return;
        }

        // Xử lý bắn (Tự động hoặc Bán tự động)
        if (isAutomatic)
        {
            if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
        else
        {
            if (Input.GetButtonDown("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
    }

    void Shoot()
    {
        // 1. Trừ đạn & Âm thanh
        currentAmmo--;
        UpdateAmmoUI();
        if (muzzleFlash != null) muzzleFlash.Play();
        if (gunAudioSource != null && fireSound != null) gunAudioSource.PlayOneShot(fireSound);
        if (gunAnimator != null) gunAnimator.SetTrigger("Fire");

        // 2. Bắn Raycast
        RaycastHit hit;
        // Kiểm tra fpsCam có null không để tránh lỗi đỏ
        if (fpsCam == null)
        {
            Debug.LogError("CHƯA GẮN CAMERA VÀO SCRIPT PLAYER SHOOTING!");
            return;
        }

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range, shootingMask))
        {
            // Debug.Log("Bắn trúng: " + hit.transform.name);

            // 3. Xử lý Trúng Zombie
            // Tìm script ZombieHealth trên đối tượng bị bắn (hoặc trên cha của nó)
            ZombieHealth target = hit.transform.GetComponent<ZombieHealth>();
            if (target == null) target = hit.transform.GetComponentInParent<ZombieHealth>();

            // Nếu tìm thấy Script Máu HOẶC Tag là Enemy
            if (target != null || hit.transform.CompareTag("Enemy"))
            {
                // A. Hiệu ứng MÁU
                if (bloodEffect != null)
                {
                    GameObject blood;
                    if (ObjectPool.Instance != null)
                    {
                        blood = ObjectPool.Instance.Get(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                        if (blood != null) ObjectPool.Instance.ReturnToPool(blood, 1f);
                    }
                    else
                    {
                        blood = Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(blood, 1f);
                    }
                }

                // B. Kiểm tra Headshot
                bool isHeadshot = IsHeadshot(hit);

                // C. Gây sát thương - Áp dụng damageMultiplier + headshot
                if (target != null)
                {
                    float finalDamage = damage * damageMultiplier * DifficultyManager.PlayerDamageMul;
                    if (isHeadshot)
                    {
                        finalDamage *= headshotMultiplier;
                        if (headshotSound != null && gunAudioSource != null)
                            gunAudioSource.PlayOneShot(headshotSound);
                    }
                    target.TakeDamage(finalDamage);

                    // Damage Number
                    if (DamageNumberManager.Instance != null)
                        DamageNumberManager.Instance.Spawn(hit.point, finalDamage, isHeadshot);

                    // Hit Marker
                    if (CrosshairUI.Instance != null)
                        CrosshairUI.Instance.ShowHitMarker(isHeadshot);
                }
            }
            else
            {
                // 4. Xử lý Trúng Tường/Đất (Hiệu ứng Tia lửa)
                if (impactEffect != null)
                {
                    GameObject impact;
                    if (ObjectPool.Instance != null)
                    {
                        impact = ObjectPool.Instance.Get(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                        if (impact != null) ObjectPool.Instance.ReturnToPool(impact, 2f);
                    }
                    else
                    {
                        impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(impact, 2f);
                    }
                }
            }

            // 5. Đẩy lùi vật lý (nếu bắn trúng đồ vật bay nhảy được)
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * 100f);
            }
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Đang nạp đạn...");

        if (gunAnimator != null) gunAnimator.SetTrigger("Reload");
        if (reloadSound != null) gunAudioSource.PlayOneShot(reloadSound);

        // Áp dụng reloadSpeedMultiplier (giá trị cao = nạp nhanh hơn = thời gian ngắn hơn)
        float actualReloadTime = reloadTime / Mathf.Max(0.1f, reloadSpeedMultiplier);
        yield return new WaitForSeconds(actualReloadTime);

        // Tính số đạn cần để đầy băng
        int needed = maxAmmo - currentAmmo;

        if (needed > 0 && inventory != null && !string.IsNullOrEmpty(ammoType))
        {
            // Xin đạn từ balo
            int taken = inventory.RequestAmmo(ammoType, needed);
            currentAmmo += taken;
        }
        else
        {
            // Không có inventory: fallback, cho full băng như cũ (phù hợp demo/scene test)
            currentAmmo = maxAmmo;
        }

        UpdateAmmoUI();
        isReloading = false;
    }

    void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = currentAmmo + " / " + maxAmmo;
        }
    }

    /// <summary>
    /// Kiểm tra có phải headshot không.
    /// Cách 1: Collider tên chứa "head" (nếu zombie có collider riêng cho đầu)
    /// Cách 2: Vị trí trúng ở phần trên cùng của zombie (trên 80% chiều cao)
    /// </summary>
    bool IsHeadshot(RaycastHit hit)
    {
        // Cách 1: Tên collider/object chứa "head"
        string hitName = hit.collider.gameObject.name.ToLower();
        if (hitName.Contains("head"))
            return true;

        // Cách 2: Hit point ở vùng đầu (phần trên 15% chiều cao zombie)
        // Tìm root zombie (có ZombieHealth)
        Transform zombieRoot = hit.transform;
        ZombieHealth zh = zombieRoot.GetComponent<ZombieHealth>();
        if (zh == null) zh = zombieRoot.GetComponentInParent<ZombieHealth>();
        if (zh == null) return false;

        Transform root = zh.transform;

        // Tính chiều cao zombie dựa trên CapsuleCollider hoặc CharacterController
        float zombieHeight = 2f; // Mặc định
        CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
        if (capsule != null)
            zombieHeight = capsule.height * root.lossyScale.y;

        // Kiểm tra hit point có nằm ở vùng đầu không (trên 85% chiều cao)
        float hitHeight = hit.point.y - root.position.y;
        float headThreshold = zombieHeight * 0.85f;

        return hitHeight >= headThreshold;
    }
}