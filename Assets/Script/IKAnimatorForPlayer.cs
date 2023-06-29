using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class IKAnimatorForPlayer : MonoBehaviour
{
    [SerializeField] private LayerMask _hitLayer;
    [SerializeField, Range(0f, 1f)] private float _distanceToGround;


    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
    }


    private void OnAnimatorIK(int layerIndex)
    {
        if (_animator)
        {
            //Position and Rotation for Left Foot
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, _animator.GetFloat("IKLeftFoot"));
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, _animator.GetFloat("IKLeftFoot"));
            //Position and Rotation for Right Foot
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, _animator.GetFloat("IKRightFoot"));
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, _animator.GetFloat("IKRightFoot"));


            //Left foot
            //Create a Ray cast with start position is leftfoot but up -> down
            RaycastHit hitInfo;
            Ray ray = new Ray(_animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
            if (Physics.Raycast(ray, out hitInfo, _distanceToGround + 1f, _hitLayer)) 
            {
                if (hitInfo.transform.gameObject.CompareTag("Walkable"))
                {
                    Vector3 footPosition = hitInfo.point; //get position of hit value => this is land point for foot

                    footPosition.y += _distanceToGround;
                    _animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                    _animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hitInfo.normal));

                }
            }

            //Right
            //Create a Ray cast with start position is right but up -> down
            ray = new Ray(_animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
            if (Physics.Raycast(ray, out hitInfo, _distanceToGround + 1f, _hitLayer))
            {
                if (hitInfo.transform.gameObject.CompareTag("Walkable"))
                {
                    Vector3 footPosition = hitInfo.point; //get position of hit value => this is land point for foot

                    footPosition.y += _distanceToGround;
                    _animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                    _animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hitInfo.normal));

                }
            }






        }
    }
}
