using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerMovement_IK : MonoBehaviour
{
    #region Variable
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
    //IK
    [Header("Foot Grounded")]
    [SerializeField] private bool _enableFeetIK = true;
    [SerializeField] private string _leftAnimatorVariableName = "LeftCurveWeight";
    [SerializeField] private string _rightAnimatorVariableName = "RightCurveWeight";
    [SerializeField] private float _heightFromGroundRaycast = 1.14f;
    [SerializeField] private float _raycastDistance = 1.5f;
    [SerializeField] private LayerMask _enviromentLayer;
    [SerializeField] private float _pelvisOffset = 0f;
    [SerializeField] private float _pelvisUpAndDownSpeed = 0.5f;
    [SerializeField] private float _footToIkPositionSpeed = 0.5f;
    [SerializeField] private bool _enableProIKFeature = true;
    [SerializeField] private bool _enableDrawRaycast = false;


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

    //IK
    private Vector3 _leftFootPosition, _rightFootPosition, _leftFootIKPosition, _rightFootIKPosition;
    private Quaternion _leftFootIKRotation, _rightFootIKRotation;
    private float _lastPelvisPositionY, _lastLeftFootPositionY, _lastRightFootPositionY;

    //animation hash
    private int _speedHash;
    private int _motionSpeedHash;

    #endregion

    #region Unity Behaviour Function
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

    /// <summary>
    /// In this Fixed Update, just only using for initial set right/left foot Position same with bone.right/leftFoot with additional HeighFromGroundRaycast
    /// Then, using this Foot Position for get the next IK Position with Rotation
    /// </summary>
    private void FixedUpdate()
    {
        if (!_enableFeetIK) return;
        if (_playerAnimator == null) return;

        //first, adjust the feet position
        AdjustFeetTarget(ref _rightFootPosition, HumanBodyBones.RightFoot);
        AdjustFeetTarget(ref _leftFootPosition, HumanBodyBones.LeftFoot);

        //cast a Raycast from the foot position to ground to check the next landing positon
        RaycastGroundSolver(_rightFootPosition, ref _rightFootIKPosition, ref _rightFootIKRotation); //right foot position, return rìght IK foot Pos and Rotation
        RaycastGroundSolver(_leftFootPosition, ref _leftFootIKPosition, ref _leftFootIKRotation); //left foot position, return left IK foot Pos and Rotation


    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (!_enableFeetIK) return;
        if (_playerAnimator == null) return;

        //Check to move pelvis to a new pelvis position
        MovePelvisHeight();

        //Set weight for IK in animation
        _playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        _playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        if (_enableProIKFeature)
        {
            _playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 
                                                    _playerAnimator.GetFloat(_rightAnimatorVariableName));
            _playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,
                                                    _playerAnimator.GetFloat(_leftAnimatorVariableName));
        }

        //Set and move feet from current position to new position
        MoveFeetToIkPoint(AvatarIKGoal.RightFoot, _rightFootIKPosition, _rightFootIKRotation, ref _lastRightFootPositionY);
        MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, _leftFootIKPosition, _leftFootIKRotation, ref _lastLeftFootPositionY);

    }
    #endregion

    #region Movement Jump Rotate Camera

    #region Movement
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

        Vector3 inputDirection = new Vector3(_movementVector2.x, 0, _movementVector2.y).normalized;

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
    #endregion

    #region Jump
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
    #endregion

    #region Rotate Camera
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
    #endregion

    #region Animation Event
    private void OnLand(AnimationEvent animationEvent)
    {

    }
    private void OnFootstep(AnimationEvent animationEvent)
    {

    }
    #endregion

    #endregion

    #region IK Foot Check ground Position

    /// <summary>
    /// this function has been called after all function has been called => all value has been set, just use and
    /// reset anything with new value
    /// </summary>
    private void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 footIKPosition, Quaternion footIKRotation, ref float lastFootPositionY)
    {
        Vector3 targetIKPositon = _playerAnimator.GetIKPosition(foot); //set target transform position as current IK foot
        
        //Check if footIKPosition has value same as Vector3.zero
        if (footIKPosition != Vector3.zero)
        {
            targetIKPositon = transform.InverseTransformPoint(targetIKPositon);
            footIKPosition = transform.InverseTransformPoint(footIKPosition);

            //value will be set from last to new value
            float yVariable = Mathf.Lerp(lastFootPositionY, footIKPosition.y, _footToIkPositionSpeed);

            targetIKPositon.y += yVariable; //this is just dimp, I want to test this
            lastFootPositionY = yVariable;
            //get point from transform point
            targetIKPositon = transform.TransformPoint(targetIKPositon);

            //Reset again
            _playerAnimator.SetIKRotation(foot, footIKRotation);

        }

        //reset again new position
        _playerAnimator.SetIKPosition(foot, targetIKPositon);
        
    }
    /// <summary>
    /// Check and reset value for height of pelvis
    /// </summary>
    private void MovePelvisHeight()
    {
        if (_leftFootIKPosition == Vector3.zero || _rightFootIKPosition == Vector3.zero
            || _lastPelvisPositionY == 0)
        {
            _lastPelvisPositionY = _playerAnimator.bodyPosition.y;
            return;
        }

        //Get left and right offset for getting totaloffset
        float leftOffsetPositionY = _leftFootIKPosition.y - transform.position.y; //always return negative value
        float rightOffsetPositionY = _rightFootIKPosition.y - transform.position.y; //alway return negative value
        float totalOffset = (leftOffsetPositionY < rightOffsetPositionY) ? leftOffsetPositionY : rightOffsetPositionY;

        //Set new pelvis postion, then, using this position to set again but set for y, and use mathf.lerp for this
        Vector3 newPelvisPosition = _playerAnimator.bodyPosition + Vector3.up * totalOffset;
        newPelvisPosition.y = Mathf.Lerp(_lastPelvisPositionY, newPelvisPosition.y, _pelvisUpAndDownSpeed);

        //set this pelvis for the body position in animator
        _playerAnimator.bodyPosition = newPelvisPosition;
        _lastPelvisPositionY = newPelvisPosition.y;


    }

    /// <summary>
    /// Draw a raycast from start position
    /// </summary>
    private void RaycastGroundSolver(Vector3 startPosition, ref Vector3 footIKPosition, ref Quaternion footIKRotation)
    {
        RaycastHit hitInfo;

        if (_enableDrawRaycast) 
            Debug.DrawLine(startPosition, startPosition + Vector3.down * (_heightFromGroundRaycast * _raycastDistance));
        
        //cast a raycast from start position with radius = raycastdistance * heightFromGroundRaycast
        if (Physics.Raycast(startPosition, Vector3.down, out hitInfo, _heightFromGroundRaycast * _raycastDistance, _enviromentLayer))
        {
            //if hit
            footIKPosition = startPosition;
            footIKPosition.y = hitInfo.point.y + _pelvisOffset;

            //set rotation new again
            //Create a rotation from this script with FromToRotation of Vector Up with hitInfo.normal
            footIKRotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * transform.rotation;

            return;
        }

        //if dont hit anything
        //just set to vector 0
        footIKPosition = Vector3.zero;
    }

    /// <summary>
    /// 
    /// </summary>
    private void AdjustFeetTarget(ref Vector3 footPosition, HumanBodyBones footBone)
    {
        footPosition = _playerAnimator.GetBoneTransform(footBone).position;
        footPosition.y = transform.position.y + _heightFromGroundRaycast;
    }


    #endregion

}
