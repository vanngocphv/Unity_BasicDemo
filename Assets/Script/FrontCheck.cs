using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontCheck : MonoBehaviour
{
    [Header("Check Info")]
    [SerializeField] private float radius;
    [SerializeField] private Transform CheckPosition;
    [SerializeField] private Transform RightHandIK;

    private Transform _weaponSelect;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }
    private void Update()
    {
        //Get all the front of collider
        CheckFrontObject();
        SelectedInteract();
    }


    private void CheckFrontObject()
    {
        if (_weaponSelect == null)
        {
            Collider[] arrayFrontCollider = Physics.OverlapSphere(CheckPosition.position, radius);

            foreach(Collider col in arrayFrontCollider)
            {
                if (col.gameObject.CompareTag("Weapon"))
                {
                    _weaponSelect = col.gameObject.transform;
                    PlayerEventManager.Instance.ShowPickupUI();
                    break;
                }
            }
        }

        else
        {
            Vector3 targetPosition = new Vector3(_weaponSelect.position.x, 0, _weaponSelect.position.z);
            Vector3 checkPosition = new Vector3(CheckPosition.position.x, 0, CheckPosition.position.z);

            if (Vector3.Distance(checkPosition, targetPosition) > radius)
            {
                _weaponSelect  = null;
                PlayerEventManager.Instance.HidePickupUI();
            }
        }
    }
    private void SelectedInteract()
    {
        if (Input.GetKeyDown(KeyCode.E) && _weaponSelect != null)
        {

            //add object to somewhere
            //Destroy(_weaponSelect.gameObject);
            //PlayerEventManager.Instance.HidePickupUI();

            _animator.SetTrigger("TriggerGrabItem");

        }
    }

    private void GrabItemPosition(AnimationEvent animationEvent)
    {
        RightHandIK.position = _weaponSelect.position;
    }
    private void GrabItemInHand(AnimationEvent animationEvent)
    {
        _weaponSelect.GetComponent<Collider>().enabled = false;
        _weaponSelect.GetComponent<Rigidbody>().isKinematic = true;
        _weaponSelect.parent = RightHandIK;
        _weaponSelect.localPosition = Vector3.zero;
        
    }
    private void GrabItemStore(AnimationEvent animationEvent)
    {
        Destroy(_weaponSelect.gameObject);
    }

}
