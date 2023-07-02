using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Value")]
    [SerializeField] private float _movementSpeed = 2.5f;
    [SerializeField] private float _sprintSpeed = 6f;
    [SerializeField] private float _rotationSmoothTime = 0.1f;
    [SerializeField] private float _speedChargeRate = 10f;

    [Header("Jump value")]
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _jumpTime = 0.4f;
    [SerializeField] private float _jumpCooldownMaxTime = 0.3f;
    [SerializeField] private float _gravity = -15.0f;

    [Header("Grounded")]
    [SerializeField] private float _groundOffset = -0.14f;
    [SerializeField] private float _groundedRadius = 0.28f;
    [SerializeField] private LayerMask _groundLayer;
    

    [Header("Camera Rotate")]
    [SerializeField] private Transform _cameraFollowTarget;
    [SerializeField] private float _threshold = 0.1f;
    [SerializeField] private float _maxLoopUp = 70f;
    [SerializeField] private float _maxLoopDown = -30f;

    [Header("Animation")]
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private AudioClip _playerClip;
    [SerializeField] private AudioClip[] _footStepClip;

    private const string CONST_STRING_SPEED = "Speed";
    private const string CONST_STRING_MOTIONSPEED = "MotionSpeed";


    private CharacterController _characterController;
    private Vector2 _movementVector2;

    private float _jumpCooldownTime = 0f;               //the jump cooldown
    private float _cameraYaw;                           //Yaw of camera
    private float _cameraPitch;                         //Pitch of camera
    private float _speed;
    private float _targetRotation;
    private float _animationBlend;
    private float _refVelocity;
    private float _verticalVelocity;
    private float _terminateVelocityVertical = 52f;
    private bool _isGround = true;

    //animation hash
    private int _speedHash;
    private int _motionSpeedHash;

    private void Awake()
    {
        
    }
    private void Start()
    {
        _characterController = this.GetComponent<CharacterController>();
        //get initial cameraYaw of this main camera
        _cameraYaw = Camera.main.transform.rotation.eulerAngles.y;

        InputManager.Instance.OnJumpButtonClicked += OnJump;

        InitialHashValue();
    }


    private void Update()
    {
        CheckGround();
        ApplyGravity();

        //movement hanle
        MovementHandle();

        //jump time handle
        if (_jumpCooldownTime > 0f) _jumpCooldownTime -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        RotateCameraHandle();
    }

    private void InitialHashValue()
    {
        _speedHash = Animator.StringToHash(CONST_STRING_SPEED);
        _motionSpeedHash = Animator.StringToHash(CONST_STRING_MOTIONSPEED);
    }
    private void MovementHandle()
    {
        _movementVector2 = InputManager.Instance.MovementVector2;
        float targetSpeed = InputManager.Instance.IsSprint ? _sprintSpeed : _movementSpeed;

        if (_movementVector2 == Vector2.zero) targetSpeed = 0;

        //Get velocity magnitude
        //float moveVelocityMagnitude = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;
        //float speedOffset = 0.1f;
        float inputMagnitude = InputManager.Instance.IsAnalogMovement ? _movementVector2.magnitude : 1f;
        if (Mathf.Abs(inputMagnitude) < 1f) 
        {
            _speed = Mathf.Lerp(_speed, targetSpeed * inputMagnitude, _speedChargeRate * Time.deltaTime);
        }
        else
        {
            _speed = targetSpeed;
        }


        //animation speed in motion
        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, _speedChargeRate * Time.deltaTime);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Vector3 inputDirection =  new Vector3(_movementVector2.x, 0, _movementVector2.y).normalized;

        if (_movementVector2 != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg 
                                + Camera.main.transform.rotation.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _refVelocity, _rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0, rotation, 0);
        }

        Vector3 targetDir = Quaternion.Euler(0, _targetRotation, 0) * Vector3.forward;
        _characterController.Move(targetDir.normalized * _speed * Time.deltaTime +
                                    new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);

        //Set animation
        _playerAnimator.SetFloat(_speedHash, _animationBlend);
        _playerAnimator.SetFloat(_motionSpeedHash, inputMagnitude);

    }
    //check if this object has been touching the ground
    private void CheckGround()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundOffset,
                transform.position.z);
        _isGround = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayer,
            QueryTriggerInteraction.Ignore);
        //_isGround = _characterController.isGrounded;
    }
    
    private void ApplyGravity()
    {
        if (_isGround)
        {
            if (_jumpCooldownTime > 0f) _jumpCooldownTime -= Time.deltaTime;
            if (_verticalVelocity < 0f) _verticalVelocity = -2f;
        }

        //
        if (_verticalVelocity < _terminateVelocityVertical) _verticalVelocity += _gravity * Time.deltaTime;

    }


    private void RotateCameraHandle()
    {
        Vector2 currentCameraPosition = InputManager.Instance.LookVector2;

        //Set yaw and pitch if magnitude > _threshold
        if (currentCameraPosition.magnitude > _threshold)
        {
            _cameraYaw += currentCameraPosition.x;
            _cameraPitch += currentCameraPosition.y;
        }

        _cameraYaw = ClampAngle(_cameraYaw, float.MinValue, float.MaxValue);
        _cameraPitch = ClampAngle(_cameraPitch, _maxLoopDown, _maxLoopUp);

        //rotate the camera with yaw and pitch, pitch for x and yaw for y (just rotate the target)
        _cameraFollowTarget.transform.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0);
    }

    private float ClampAngle(float angle, float min, float max)
    {
        //Clamp the data in range [min, max]
        if (angle > 360f) angle -= 360f;
        else if (angle < -360f) angle += 360f;

        angle = Mathf.Clamp(angle, min, max);
        return angle;
    }

    private void OnJump()
    {
        if (_isGround && _jumpCooldownTime <= 0f)
        {
            //canjump again
            //Set a jump velocity
            _verticalVelocity = Mathf.Sqrt(_gravity * -2f * _jumpHeight);


            _jumpCooldownTime = _jumpCooldownMaxTime;
        }
        else return;
    }

    private void OnLand(AnimationEvent animationEvent)
    {

    }
    private void OnFootstep(AnimationEvent animationEvent)
    {

    }
}
