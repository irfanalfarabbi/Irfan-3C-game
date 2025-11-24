using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{

    private void Update()
    {
        CheckMovementInput();
        CheckMovementCameraInput();
        CheckSprintInput();
        CheckJumpInput();
        CheckClimbInput();
        CheckChangePOVInput();
        CheckCrouchInput();
        CheckGlideInput();
        CheckCancelInput();
        CheckPunchInput();
    }

    public Action<Vector2> OnMoveInput;
    private void CheckMovementInput()
    {
        float horizontalAxis = Input.GetAxis("Horizontal");
        float verticalAxis = Input.GetAxis("Vertical");
        Vector2 inputAxis = new Vector2(horizontalAxis, verticalAxis);
        bool isMoveInput = inputAxis != Vector2.zero;
        if (isMoveInput)
        {
            if (OnMoveInput != null)
            {
                OnMoveInput(inputAxis);
            }
        }
    }

    public Action<Vector2> OnMoveCameraInput;
    private void CheckMovementCameraInput()
    {
        float horizontalAxis = Input.GetAxis("Mouse X");
        float verticalAxis = Input.GetAxis("Mouse Y");
        Vector2 inputAxis = new Vector2(horizontalAxis, verticalAxis);
        // bool isMoveInput = inputAxis != Vector2.zero || Input.GetAxis("MouseX") != 0f;
        bool isMoveCameraInput = inputAxis != Vector2.zero;
        if (isMoveCameraInput)
        {
            if (OnMoveCameraInput != null)
            {
                OnMoveCameraInput(inputAxis);
            }
        }
    }

    public Action<bool> OnSprintInput;
    private void CheckSprintInput()
    {
        bool isHoldSprintInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (isHoldSprintInput)
        {
            if (OnSprintInput != null)
            {
                OnSprintInput(true);
            }
        }
        else
        {
            if (OnSprintInput != null)
            {
                OnSprintInput(false);
            }
        }
    }

    public Action OnJumpInput;
    private void CheckJumpInput()
    {
        bool isPressJumpInput = Input.GetKeyDown(KeyCode.Space);
        if (isPressJumpInput)
        {
            if (OnJumpInput != null)
            {
                OnJumpInput();
            }
        }
    }

    public Action OnClimbInput;
    private void CheckClimbInput()
    {
        bool isPressClimbInput = Input.GetKeyDown(KeyCode.E);
        if (isPressClimbInput)
        {
            if (OnClimbInput != null)
            {
                OnClimbInput();
            }
        }
    }

    public Action OnChangePOV;
    private void CheckChangePOVInput()
    {
        bool isPressChangePOVInput = Input.GetKeyDown(KeyCode.Q);
        if (isPressChangePOVInput)
        {
            if (OnChangePOV != null)
            {
                OnChangePOV();
            }
        }
    }

    public Action OnCrouchInput;
    private void CheckCrouchInput()
    {
        bool isPressCrouchInput = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
        if (isPressCrouchInput)
        {
            if (OnCrouchInput != null)
            {
                OnCrouchInput();
            }
        }
    }

    public Action OnGlideInput;
    private void CheckGlideInput()
    {
        bool isPressGlideInput = Input.GetKeyDown(KeyCode.G);
        if (isPressGlideInput)
        {
            if (OnGlideInput != null)
            {
                OnGlideInput();
            }
        }
    }

    public Action OnCancelClimb;
    public Action OnCancelGlide;
    private void CheckCancelInput()
    {
        bool isPressCancelInput = Input.GetKeyDown(KeyCode.C);
        if (isPressCancelInput)
        {
            if (OnCancelClimb != null)
            {
                OnCancelClimb();
            }
            if (OnCancelGlide != null)
            {
                OnCancelGlide();
            }
        }
    }

    public Action OnPunchInput;
    private void CheckPunchInput()
    {
        bool isPressPunchInput = Input.GetKeyDown(KeyCode.Mouse0);
        if (isPressPunchInput)
        {
            // Debug.Log("CheckPunchInput!!");
            if (OnPunchInput != null)
            {
                OnPunchInput();
            }
        }
    }
}
