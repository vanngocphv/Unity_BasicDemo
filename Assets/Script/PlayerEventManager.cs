using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEventManager : MonoBehaviour
{
    public static PlayerEventManager Instance;

    [SerializeField] private PickupUI pickupUI;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowPickupUI()
    {
        pickupUI.ShowGameObject();
    }
    public void HidePickupUI()
    {
        pickupUI.HideGameObject();
    }

}
