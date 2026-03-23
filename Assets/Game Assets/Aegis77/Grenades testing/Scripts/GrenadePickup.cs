using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aegis.GrenadeSystem.HiEx
{
    public class GrenadePickup : MonoBehaviour
    {

        // this script handles picking up a grenade, and should be attatched to the grenade pickup
        // You can duplicate it for different types of grenade, to add them to an inventory system

        [SerializeField] AudioClip grenadePickupSound;

        //this logic is what happens when a palyer picks up a grenade with this script attatched
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Tìm GrenadeSystem trên Player hoặc con
                GrenadeSystem gs = other.GetComponent<GrenadeSystem>();
                if (gs == null) gs = other.GetComponentInChildren<GrenadeSystem>();
                if (gs == null) gs = other.GetComponentInParent<GrenadeSystem>();

                if (gs != null)
                {
                    gs.PickupGrenade();

                    // Phát âm thanh
                    if (grenadePickupSound != null)
                    {
                        AudioSource.PlayClipAtPoint(grenadePickupSound, transform.position);
                    }

                    Destroy(gameObject);
                }
            }
        }

    }
}