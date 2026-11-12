using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : ManagerBase
{
    public enum Map
    {
        Player,
        UI
    }

    private const string ControlsAddress = "GameControls";

    private ResourceManager _resourceManager;
    private InputActionAsset _controls;
    private bool _bound;
    private bool _alive = true;
    private InputActionMap _playerMap;
    private InputActionMap _uiMap;
    private InputAction _move;
    private InputAction _look;
    private InputAction _leftClick;
    private InputAction _rightClick;
    private InputAction _uiPoint;
    private InputAction _uiClick;
    private InputAction _uiRightClick;
    private InputAction _uiCancel;

    public Map CurrentMap { get; private set; } = Map.Player;

    public Vector2 Move { get; private set; }
    public Vector2 PointerPosition { get; private set; }
    public bool IsLeftClickHeld { get; private set; }
    public bool IsRightClickHeld { get; private set; }
    public Vector2 UIPointerPosition { get; private set; }
    public bool IsUIClickHeld { get; private set; }

    public event Action<Vector2> MoveChanged;
    public event Action<Vector2> LookChanged;
    public event Action LeftClickStarted;
    public event Action LeftClickCanceled;
    public event Action RightClickStarted;
    public event Action RightClickCanceled;
    public event Action<Vector2> UIPointChanged;
    public event Action UIClickStarted;
    public event Action UIClickCanceled;
    public event Action UIRightClickStarted;
    public event Action UICancelStarted;

    private void Awake()
    {
        var gameManager = GetComponentInParent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[InputManager] GameManager 아래에서만 사용할 수 있습니다.", this);
            return;
        }

        LoadControlsAsync(gameManager.ResourceManager);
    }

    private async void LoadControlsAsync(ResourceManager resourceManager)
    {
        await LoadControls(resourceManager);
    }

    private async Task LoadControls(ResourceManager resourceManager)
    {
        if (resourceManager == null || _bound)
            return;

        _resourceManager = resourceManager;
        var controlsAsset = await resourceManager.LoadAsync<InputActionAsset>(ControlsAddress);
        if (!_alive)
        {
            resourceManager.Release(ControlsAddress);
            return;
        }

        if (controlsAsset == null)
        {
            Debug.LogError("[InputManager] GameControls 에셋이 없습니다.", this);
            return;
        }

        _controls = Instantiate(controlsAsset);
        _playerMap = _controls.FindActionMap("Player", true);
        _uiMap = _controls.FindActionMap("UI", true);
        _move = _playerMap.FindAction("Move", true);
        _look = _playerMap.FindAction("Look", true);
        _leftClick = _playerMap.FindAction("LeftClick", true);
        _rightClick = _playerMap.FindAction("RightClick", true);
        _uiPoint = _uiMap.FindAction("Point", true);
        _uiClick = _uiMap.FindAction("Click", true);
        _uiRightClick = _uiMap.FindAction("RightClick", true);
        _uiCancel = _uiMap.FindAction("Cancel", true);
        _bound = true;

        if (isActiveAndEnabled)
            Subscribe();
    }

    private void OnEnable()
    {
        if (!_bound)
            return;

        Subscribe();
    }

    private void Subscribe()
    {
        _move.performed += OnMove;
        _move.canceled += OnMove;
        _look.performed += OnLook;
        _look.canceled += OnLook;
        _leftClick.started += OnLeftClickStarted;
        _leftClick.canceled += OnLeftClickCanceled;
        _rightClick.started += OnRightClickStarted;
        _rightClick.canceled += OnRightClickCanceled;
        _uiPoint.performed += OnUIPoint;
        _uiPoint.canceled += OnUIPoint;
        _uiClick.started += OnUIClickStarted;
        _uiClick.canceled += OnUIClickCanceled;
        _uiRightClick.started += OnUIRightClickStarted;
        _uiCancel.started += OnUICancelStarted;

        EnableMap(CurrentMap);
    }

    private void OnDisable()
    {
        if (!_bound)
            return;

        Unsubscribe();
    }

    private void Unsubscribe()
    {
        _move.performed -= OnMove;
        _move.canceled -= OnMove;
        _look.performed -= OnLook;
        _look.canceled -= OnLook;
        _leftClick.started -= OnLeftClickStarted;
        _leftClick.canceled -= OnLeftClickCanceled;
        _rightClick.started -= OnRightClickStarted;
        _rightClick.canceled -= OnRightClickCanceled;
        _uiPoint.performed -= OnUIPoint;
        _uiPoint.canceled -= OnUIPoint;
        _uiClick.started -= OnUIClickStarted;
        _uiClick.canceled -= OnUIClickCanceled;
        _uiRightClick.started -= OnUIRightClickStarted;
        _uiCancel.started -= OnUICancelStarted;

        _playerMap.Disable();
        _uiMap.Disable();
    }

    private void OnDestroy()
    {
        _alive = false;
        if (_bound && isActiveAndEnabled)
            Unsubscribe();

        if (_controls != null)
            Destroy(_controls);

        if (_bound)
            _resourceManager?.Release(ControlsAddress);
    }

    public void EnableMap(Map map)
    {
        if (_controls == null)
            return;

        CurrentMap = map;
        if (map == Map.Player)
        {
            _uiMap.Disable();
            _playerMap.Enable();
            return;
        }

        _playerMap.Disable();
        _uiMap.Enable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
        MoveChanged?.Invoke(Move);
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        PointerPosition = context.ReadValue<Vector2>();
        LookChanged?.Invoke(PointerPosition);
    }

    private void OnLeftClickStarted(InputAction.CallbackContext context)
    {
        IsLeftClickHeld = true;
        LeftClickStarted?.Invoke();
    }

    private void OnLeftClickCanceled(InputAction.CallbackContext context)
    {
        IsLeftClickHeld = false;
        LeftClickCanceled?.Invoke();
    }

    private void OnRightClickStarted(InputAction.CallbackContext context)
    {
        IsRightClickHeld = true;
        RightClickStarted?.Invoke();
    }

    private void OnRightClickCanceled(InputAction.CallbackContext context)
    {
        IsRightClickHeld = false;
        RightClickCanceled?.Invoke();
    }

    private void OnUIPoint(InputAction.CallbackContext context)
    {
        UIPointerPosition = context.ReadValue<Vector2>();
        UIPointChanged?.Invoke(UIPointerPosition);
    }

    private void OnUIClickStarted(InputAction.CallbackContext context)
    {
        IsUIClickHeld = true;
        UIClickStarted?.Invoke();
    }

    private void OnUIClickCanceled(InputAction.CallbackContext context)
    {
        IsUIClickHeld = false;
        UIClickCanceled?.Invoke();
    }

    private void OnUIRightClickStarted(InputAction.CallbackContext context)
    {
        UIRightClickStarted?.Invoke();
    }

    private void OnUICancelStarted(InputAction.CallbackContext context)
    {
        UICancelStarted?.Invoke();
    }
}
