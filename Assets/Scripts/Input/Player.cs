using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour {
    public delegate void PlayerActionEvent(ActionType type);
    public event PlayerActionEvent OnPlayerAction;

    private PlayerInput input;

    private Vector2 _moveInput = Vector2.zero;
    private bool _jumpInput = false;
    private bool _jumpCancelled = false;

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
     *  PLAYER ACTIONS
     */
    public void OnMovement(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started) _jumpInput = true;
        if (ctx.canceled) _jumpCancelled = true;
        print($"Input: {_jumpInput}, Cancelled: {_jumpCancelled}");
    }
    public void OnLightAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnPlayerAction?.Invoke(ActionType.ATTACK_LIGHT);
    }
    public void OnHeavyAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnPlayerAction?.Invoke(ActionType.ATTACK_HEAVY);
    }
    public void OnSpecial(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnPlayerAction?.Invoke(ActionType.SPECIAL);
    }
    public void OnDash(InputAction.CallbackContext ctx)
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
    public void OnNavigate(InputAction.CallbackContext ctx)
    {
        Vector2 navigate = ctx.ReadValue<Vector2>();
        print($"Navigating {navigate}");
    }
    public void OnConfirm(InputAction.CallbackContext ctx)
    {
        print("Confirming");
    }
    public void OnCancel(InputAction.CallbackContext ctx)
    {
        print("Cancelling");
    }
}
