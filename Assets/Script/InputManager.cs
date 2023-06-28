using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public event System.Action OnJumpButtonClicked;
    public Vector2 MovementVector2 => _inputMovement;
    public Vector2 LookVector2 => _lookCameraRotation;
    public bool IsSprint => _isSprint;
    public bool IsAnalogMovement => _isAnalogMovement;

    [SerializeField] private bool _isAnalogMovement = false;

    private PlayerInputAction _playerInputAction;
    private Vector2 _inputMovement;
    private Vector2 _lookCameraRotation;
    private bool _isSprint = false;


    private void Awake()
    {
        Instance = this;
        _playerInputAction = new PlayerInputAction();
    }
    private void Start()
    {
        //Movement
        _playerInputAction.Input.Movement.started += OnMovementSetting;
        _playerInputAction.Input.Movement.performed += OnMovementSetting;
        _playerInputAction.Input.Movement.canceled += OnMovementSetting;
        //Sprint
        _playerInputAction.Input.Sprint.started += OnSprintSetting;
        _playerInputAction.Input.Sprint.performed += OnSprintSetting;
        _playerInputAction.Input.Sprint.canceled += OnSprintSetting;
        //Jump
        _playerInputAction.Input.Jump.started += OnJumpClicked;
        //Look rotation
        _playerInputAction.Input.Looking.started += OnCameraRotate;
        _playerInputAction.Input.Looking.performed += OnCameraRotate;
        _playerInputAction.Input.Looking.canceled += OnCameraRotate;


    }

#if ENABLE_INPUT_SYSTEM
    private void OnMovementSetting(InputAction.CallbackContext ctx)
    {
        _inputMovement = ctx.ReadValue<Vector2>();
    }
    private void OnSprintSetting(InputAction.CallbackContext ctx)
    {
        _isSprint = ctx.ReadValueAsButton();
    }
    private void OnJumpClicked(InputAction.CallbackContext ctx)
    {
        //Invoke event, not setting for bool when the player script want to check if jump has been clicked
        OnJumpButtonClicked?.Invoke();
    }
    private void OnCameraRotate(InputAction.CallbackContext ctx)
    {
        _lookCameraRotation = ctx.ReadValue<Vector2>();
    }
#endif

    private void OnEnable()
    {
        _playerInputAction.Enable();
    }
    private void OnDisable()
    {
        _playerInputAction.Disable();
    }
    private void OnDestroy()
    {
        //Movement
        _playerInputAction.Input.Movement.started -= OnMovementSetting;
        _playerInputAction.Input.Movement.performed -= OnMovementSetting;
        _playerInputAction.Input.Movement.canceled -= OnMovementSetting;
        //Sprint
        _playerInputAction.Input.Sprint.started -= OnSprintSetting;
        _playerInputAction.Input.Sprint.performed -= OnSprintSetting;
        _playerInputAction.Input.Sprint.canceled -= OnSprintSetting;
        //Jump
        _playerInputAction.Input.Jump.started -= OnJumpClicked;
        //Look rotation
        _playerInputAction.Input.Looking.started -= OnCameraRotate;
        _playerInputAction.Input.Looking.performed -= OnCameraRotate;
        _playerInputAction.Input.Looking.canceled -= OnCameraRotate;
    }
}
