using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aegis.GrenadeSystem.HiEx
{
    public class HiExGrenade : MonoBehaviour
    {
        // Explosion effects
        [Header("Explosion Effects")]
        [SerializeField] GameObject explosionEffectPrefab;
        [SerializeField] Vector3 explosionParticleOffset = new Vector3(0, 1, 0);


        //explosion settings
        [Header("Explosion Settings")]
        [SerializeField] float explosionDelay = 3f;
        [SerializeField] float explosionForce = 1000f;
        [SerializeField] float explosionForceRadius = 5f;

        // Damage settings
        [Header("Damage Settings")]
        [SerializeField] float closeRadius = 3f;
        [SerializeField] float nearRadius = 6f;
        [SerializeField] float farRadius = 10f;

        [SerializeField] float closeDam = 200f;
        [SerializeField] float nearDam = 100f;
        [SerializeField] float farDam = 40f;


        // Audio effects
        [Header("Audio Effects")]
        [SerializeReference] GameObject audioSourcePrefab;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip impact;
        [SerializeField] AudioClip[] explosionSounds;

        // internal variables
        float countdown;
        bool hasexploded = false;

        private void Awake()
        {
            audioSource = gameObject.GetComponent<AudioSource>();
        }


        private void Start()
        {
            // set the timer
            countdown = explosionDelay;
        }

        private void Update()
        {
            // if the grenade hasn't exploded, reduce the timer
            if (!hasexploded)
            {
                countdown -= Time.deltaTime;
                if (countdown <= 0)
                {
                    Explode();
                    hasexploded = true;
                }
            }


        }


        //explode function - what happens when the timer reaches 0
        void Explode()
        {

            // instantiate explosion effect at this game object
            GameObject explosionEffect = Instantiate(explosionEffectPrefab, transform.position + explosionParticleOffset, Quaternion.identity);

            Destroy(explosionEffect, 1.9f);

            PlaySoundAtPosition();

            ApplyExplosiveForce();

            ApplyDamage();

            Destroy(gameObject);
        }


        //Function to apply damage - tích hợp với hệ thống ZombieHealth/PlayerHealth
        void ApplyDamage()
        {
            Debug.Log($"[Grenade] BOOM tại {transform.position} | closeR={closeRadius} nearR={nearRadius} farR={farRadius} | closeDmg={closeDam} nearDmg={nearDam} farDmg={farDam}");

            // Dùng 1 vòng OverlapSphere lớn nhất, tính damage theo khoảng cách
            Collider[] allColliders = Physics.OverlapSphere(transform.position, farRadius);

            Debug.Log($"[Grenade] Tìm thấy {allColliders.Length} collider trong bán kính {farRadius}m");

            // Tránh gây damage trùng lặp cho cùng 1 entity (dùng root transform ID)
            HashSet<int> damaged = new HashSet<int>();

            foreach (Collider col in allColliders)
            {
                // Dùng root ID để tránh trùng khi 1 entity có nhiều collider
                Transform root = col.transform.root;
                int id = root.gameObject.GetInstanceID();
                if (damaged.Contains(id)) continue;

                // Tính khoảng cách đến điểm gần nhất trên collider (chính xác hơn)
                float dist = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
                float dmg = 0f;

                if (dist <= closeRadius) dmg = closeDam;
                else if (dist <= nearRadius) dmg = nearDam;
                else if (dist <= farRadius) dmg = farDam;

                if (dmg <= 0f) continue;

                Debug.Log($"[Grenade] Hit: {col.gameObject.name} tag={col.tag} dist={dist:F2}m dmg={dmg}");

                // Gây damage cho Zombie
                if (col.CompareTag("Enemy") || root.CompareTag("Enemy"))
                {
                    ZombieHealth zh = root.GetComponent<ZombieHealth>();
                    if (zh == null) zh = col.GetComponentInParent<ZombieHealth>();
                    if (zh != null)
                    {
                        zh.TakeDamage(dmg);
                        damaged.Add(id);

                        // Damage Number
                        if (DamageNumberManager.Instance != null)
                            DamageNumberManager.Instance.Spawn(col.transform.position + Vector3.up * 1.5f, dmg, false);
                    }
                }

                // Gây damage cho Player / Ally
                if (col.CompareTag("Player") || root.CompareTag("Player"))
                {
                    PlayerHealth ph = root.GetComponent<PlayerHealth>();
                    if (ph == null) ph = col.GetComponentInParent<PlayerHealth>();
                    if (ph != null)
                    {
                        ph.TakeDamage(dmg);
                        damaged.Add(id);
                        Debug.Log($"[Grenade] Player nhận {dmg} damage! dist={dist:F2}m");
                    }

                    // Cũng check AllyHealth
                    AllyHealth ah = root.GetComponent<AllyHealth>();
                    if (ah == null) ah = col.GetComponentInParent<AllyHealth>();
                    if (ah != null)
                    {
                        ah.TakeDamage(dmg);
                        damaged.Add(id);
                        Debug.Log($"[Grenade] Ally nhận {dmg} damage! dist={dist:F2}m");
                    }
                }
            }
        }


        //Function to apply physics explosive force to objects near the explosion
        void ApplyExplosiveForce()
        {
            //Create a list of all colliders of objects within the radius of the explosion force
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionForceRadius);

            //for every collider collected, apply an explosive force originating from the position of the explosion
            foreach (Collider nearbyobject in colliders)
            {
                Rigidbody rb = nearbyobject.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionForceRadius);
                }
            }
        }

        //Function to play explosion sound effect by instantiating a new object to play that sound at the explosion
        void PlaySoundAtPosition()
        {
            GameObject audiosourceObject = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);

            int rand = Random.Range(0, explosionSounds.Length);

            AudioSource instantiatedAudioSource = audiosourceObject.GetComponent<AudioSource>();

            instantiatedAudioSource.spatialBlend = 1;
            instantiatedAudioSource.clip = explosionSounds[rand];
            instantiatedAudioSource.Play();

            Destroy(audiosourceObject, instantiatedAudioSource.clip.length);


        }

        //Function to play an impact sound effect if the thrown grenade hits something, but has not exploded yet
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag != "Player")
            {
                audioSource.clip = impact;

                audioSource.spatialBlend = 1;

                audioSource.Play();
            }

        }

    }

}
