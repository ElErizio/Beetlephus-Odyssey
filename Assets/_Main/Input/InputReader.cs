using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, Input.IDefaultActions
{
    public Vector2 MovementValue { get; private set; }
    private Input _controls;
    
    private void Start()
    {
        _controls = new Input();
        _controls.Default.SetCallbacks(this);
        _controls.Default.Enable();
    }

    private void OnDestroy()
    {
        _controls.Default.Disable();
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        MovementValue = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
    }
}
