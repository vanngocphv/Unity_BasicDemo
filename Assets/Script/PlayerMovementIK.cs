using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerMovementIK : MonoBehaviour
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
    [SerializeField] private bool _enableFootIK = true;
    [SerializeField] private bool _enableFootIKProFeature = true;
    [SerializeField] private bool _enableDrawRaycast = false;
    [SerializeField] private float _raycastDistance = 1.14f;
    [SerializeField] private float _heightFromGroundRaycast = 1.5f;
    [SerializeField] private float _pelvisUpAndDownSpeed = 0.5f;
    [SerializeField] private float _footToIKPositionSpeed = 0.5f;
    [SerializeField] private float _pelvisOffset = 0.0f;
    [SerializeField] private LayerMask _enviromentLayer;
    [SerializeField] private string _leftFootAnimtorVariableName = "IKLeftFoot";
    [SerializeField] private string _rightFootAnimtorVariableName = "IKRightFoot";




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
    private Vector3 _leftFootPosition, _rightFootPosition, _leftIKFootPosition, _rightIKFootPosition;
    private Quaternion _leftIKFootRotation, _rightIKFootRotation;
    private float _lastLeftFootPositionY, _lastRightFootPositionY, _lastPelvisPositionY;

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
    /// every fixed update frame, call adjust the foot position -> check the ground if raycast hit anything, solver
    /// and reset the value for IK position and IK Rotation
    /// </summary>
    private void FixedUpdate()
    {
        //check if Enable foot IK = false;
        if (_enableFootIK == false) return;
        //check if animator still doesn't set
        if (_playerAnimator == null) return;

        //adjust the position, adjust to correct position
        AdjustFeetTarget(ref _leftFootPosition, HumanBodyBones.LeftFoot);
        AdjustFeetTarget(ref _rightFootPosition, HumanBodyBones.RightFoot);

        //Solver the landing position for foot when raycast hit anything in enviroment Layer
        FeetPositionSolver(_leftFootPosition, ref _leftIKFootPosition, ref _leftIKFootRotation);
        FeetPositionSolver(_rightFootPosition, ref _rightIKFootPosition, ref _rightIKFootRotation);

    }

    private void OnAnimatorIK(int layerIndex)
    {
        //check if Enable foot IK = false;
        if (_enableFootIK == false) return;
        //check if animator still doesn't set
        if (_playerAnimator == null) return;

        //set new pelvis height
        //just using for move the pelvis to new position if has
        MovePelvisHeight();

        //set weight for Ik position
        _playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        _playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);

        //check if enable pro IK feature
        if (_enableFootIKProFeature)
        {

            _playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 
                                                    _playerAnimator.GetFloat(_rightFootAnimtorVariableName));
            _playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,
                                                    _playerAnimator.GetFloat(_leftFootAnimtorVariableName));
        }

        //total value has been set, move the feet to IK position
        //value get from feet position solver function, and the last value just using for move from it to new it
        MoveFeetToIKPoint(AvatarIKGoal.LeftFoot, _leftIKFootPosition, _leftIKFootRotation, ref _lastLeftFootPositionY);
        MoveFeetToIKPoint(AvatarIKGoal.RightFoot, _rightIKFootPosition, _rightIKFootRotation, ref _lastRightFootPositionY);
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

    #region Ground Checking for handling IK
    /// <summary>
    /// this function will be called after all value has been set in program
    /// the feet will be place correctly
    /// </summary>
    private void MoveFeetToIKPoint(AvatarIKGoal footIK, Vector3 footIKHolderPosition, Quaternion footIKRotation, ref float lastFootPositionY)
    {
        //Get the target position = value of the bone from animationIK
        Vector3 targetPosition = _playerAnimator.GetIKPosition(footIK);

        //if the footIKHolderPosition has value <> Vector3.zero, inverse target position and footIKHolderPosition
        if (footIKHolderPosition != Vector3.zero)
        {
            targetPosition = transform.InverseTransformPoint(targetPosition);
            footIKHolderPosition = transform.InverseTransformPoint(footIKHolderPosition);

            //Get y variable for add and using for set last y position
            float yVariable = Mathf.Lerp(lastFootPositionY, footIKHolderPosition.y, _footToIKPositionSpeed);
            targetPosition.y += yVariable;
            lastFootPositionY = yVariable;

            Debug.Log("Target Position current: " + targetPosition);
            //tranfrom target position to transfrom point position in world space
            targetPosition = transform.TransformPoint(targetPosition);
            Debug.Log("Target Transfrom Point Position: " + targetPosition);

            //Set new rotation from input rotaion
            _playerAnimator.SetIKRotation(footIK, footIKRotation);

        }

        //Set new target position for foot IK
        _playerAnimator.SetIKPosition(footIK, targetPosition);
    }

    /// <summary>
    /// the pelvis will be moved every Fixed Frame, with new height 
    /// </summary>
    private void MovePelvisHeight()
    {
        //Reset last pelvis position
        //Check the last position of pelvis if set or not => reset again if available
        //Check if Left/Right Ik is base animation position (Vector3.zero) reset last pelvis position
        if (_lastPelvisPositionY == 0 || _leftIKFootPosition == Vector3.zero || _rightIKFootPosition == Vector3.zero)
        {
            _lastPelvisPositionY = _playerAnimator.bodyPosition.y;
            return;
        }

        //Get offset for left and right, then compare to get the longest length
        float leftOffset = _leftIKFootPosition.y - transform.position.y;
        float rightOffset = _rightIKFootPosition.y - transform.position.y;
        float totalOffset = (leftOffset < rightOffset) ? leftOffset : rightOffset;

        //from total offset, calculate new pelvis position
        Vector3 newPelvisPosition = _playerAnimator.bodyPosition + Vector3.up * totalOffset;
        //Re calculate new Y position for new pelvis position, this is need to change frame by frame, for smooth purpose
        newPelvisPosition.y = Mathf.Lerp(_lastPelvisPositionY, newPelvisPosition.y, _pelvisUpAndDownSpeed);
        _playerAnimator.bodyPosition = newPelvisPosition;
        _lastPelvisPositionY = newPelvisPosition.y;


    }

    /// <summary>
    /// Cast a raycast from start position to ground, if hit Enviroment layer => return a new landing position
    /// </summary>
    private void FeetPositionSolver(Vector3 startPosition, ref Vector3 footIKPosition, ref Quaternion footIKRotation)
    {
        RaycastHit hitInfo; //hit information

        //apply draw raycast for check and debug
        if (_enableDrawRaycast)
            Debug.DrawLine(startPosition, startPosition + Vector3.down * (_heightFromGroundRaycast * _raycastDistance));

        //cast a raycast to check the ground
        if (Physics.Raycast(startPosition, Vector3.down, out hitInfo, _heightFromGroundRaycast * _raycastDistance,
                                _enviromentLayer))
        {
            //set current IK position same data with start position
            footIKPosition = startPosition;
            footIKPosition.y = hitInfo.point.y + _pelvisOffset;

            //set a new rotation value
            footIKRotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * transform.rotation;
            return; //end the setting 
        }

        //check ground fail
        footIKPosition = Vector3.zero;
    }
    private void AdjustFeetTarget(ref Vector3 _feetPosition, HumanBodyBones footBone)
    {
        _feetPosition = _playerAnimator.GetBoneTransform(footBone).position;
        _feetPosition.y = transform.position.y + _heightFromGroundRaycast;
    }

    #endregion
}
