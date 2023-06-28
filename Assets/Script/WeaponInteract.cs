using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponInteract : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerEventManager.Instance.ShowPickupUI();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerEventManager.Instance.HidePickupUI();
        }
    }
}
