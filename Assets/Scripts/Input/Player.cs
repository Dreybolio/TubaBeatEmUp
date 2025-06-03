using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
    public delegate void PlayerActionEvent(ActionType type);
    public event PlayerActionEvent OnPlayerAction;

    public delegate void UINavigateEvent(Vector2 vec);
    public delegate void UIEvent();
    public event UINavigateEvent OnUINavigate;
    public event UIEvent OnUIConfirm;
    public event UIEvent OnUICancel;

    private PlayerInput input;

    // Assigned Data at runtime
    [NonSerialized] public CharacterData CharacterData;
    [NonSerialized] public PlayerController Controller;

    // Variables
    private Vector2 _moveInput = Vector2.zero;
    private bool _jumpInput = false;
    private bool _jumpCancelled = false;
    private bool _uiNavigateCancel = true;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        input = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        input.actions.Enable();
    }
    private void OnDisable()
    {
        input.actions.Disable();
    }
    public void SetActionMap(string map)
    {
        input.SwitchCurrentActionMap(map);
    }

    /*
     *  Player Spawning
     */
    public PlayerController SpawnPlayerController()
    {
        if (CharacterData != null)
        {
            if (Controller == null)
            {
                Controller = Instantiate((PlayerController)CharacterData.spawnablePrefab);
                Controller.name = $"{name} Controller";
                Controller.AssignPlayer(this);
                return Controller;
            }
            else
            {
                Debug.LogError($"{name}: Tried to spawn a physical player when one already exists");
                return null;
            }
        }
        Debug.LogError($"{name}: Tried to spawn a physical player with no data");
        return null;
    }

    /*
     *  PLAYER ACTIONS
     */
    public void Movement(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }
    public void Jump(InputAction.CallbackContext ctx)
    {
        if (ctx.started) _jumpInput = true;
        if (ctx.canceled) _jumpCancelled = true;
        print($"Input: {_jumpInput}, Cancelled: {_jumpCancelled}");
    }
    public void LightAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnPlayerAction?.Invoke(ActionType.ATTACK_LIGHT);
    }
    public void HeavyAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnPlayerAction?.Invoke(ActionType.ATTACK_HEAVY);
    }
    public void Special(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnPlayerAction?.Invoke(ActionType.SPECIAL);
    }
    public void Dash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnPlayerAction?.Invoke(ActionType.DASH);
    }

    //Getters
    public Vector2 GetPlayerMovement()
    {
        return _moveInput;
    }
    public bool GetPlayerJump()
    {
        bool tmp = _jumpInput;
        _jumpInput = false;
        return tmp;
    }
    public bool GetPlayerJumpReleased()
    {
        bool tmp = _jumpCancelled;
        _jumpCancelled = false;
        return tmp;
    }

    /*
    *  UI ACTIONS
    */
    public void UINavigate(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && _uiNavigateCancel)
        {
            Vector2 navigate = ctx.ReadValue<Vector2>();
            _uiNavigateCancel = false;
            OnUINavigate?.Invoke(navigate);
        }
        else if (ctx.canceled)
        {
            _uiNavigateCancel = true;
        }
    }
    public void UIConfirm(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnUIConfirm?.Invoke();
    }
    public void UICancel(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnUICancel?.Invoke();
    }
}
