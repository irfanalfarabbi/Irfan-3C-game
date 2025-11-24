using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    /// Methods
    // References
    [SerializeField]
    private InputManager _input;
    private Rigidbody _rigidbody;
    private PlayerStance _playerStance;
    private CapsuleCollider _collider;
    private Animator _animator;

    // Cameras
    [SerializeField]
    private CameraManager _cameraManager;
    [SerializeField]
    private Transform _cameraTransform;
    [SerializeField]
    private float _cameraHorizontalRotationSpeed;

    // Walking, Running, Slope, Crouch
    [SerializeField]
    private float _walkSpeed;
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _crouchSpeed;
    private float _speed;
    [SerializeField]
    private float _walkSprintTransition;
    [SerializeField]
    private Vector3 _upperStepOffset;
    [SerializeField]
    private float _stepCheckerDistance;
    [SerializeField]
    private float _stepForce;
    [SerializeField]
    private float _rotationSmoothTime = 0.1f;
    private float _rotationSmoothVelocity;

    // Jumping
    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private float _groundDetectorRadius;
    [SerializeField]
    private LayerMask _groundLayer;
    [SerializeField]
    private float _jumpForce;
    private bool _isGrounded;

    // Climbing
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private float _climbDetectorDistance;
    [SerializeField]
    private LayerMask _climbableLayer;
    [SerializeField]
    private float _climbSpeed;
    [SerializeField]
    private float _climbVelocityMax;
    [SerializeField]
    private float _climbInitForce;
    [SerializeField]
    private float _climbStickForce;
    private bool _isInFrontOfClimbingWall;
    private RaycastHit _raycastHit;

    // Gliding
    [SerializeField]
    private float _glideSpeed;
    [SerializeField]
    private float _airDrag;
    [SerializeField]
    private Vector3 _glideRotationSpeed;
    [SerializeField]
    private float _minGlideRotationX;
    [SerializeField]
    private float _maxGlideRotationX;

    // Punching
    private bool _isPunching;
    private int _combo = 0;
    [SerializeField]
    private float _resetComboInterval;
    private Coroutine _resetCombo;
    [SerializeField]
    private Transform _hitDetector;
    [SerializeField]
    private float _hitDetectorRadius;
    [SerializeField]
    private LayerMask _hitLayer;

    /// Messages
    // 1. Awake, Unity calls Awake when loading an instance of a script component.
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<CapsuleCollider>();
        _speed = _walkSpeed;
        _playerStance = PlayerStance.Stand;
        HideAndLockCursor();
    }

    // 2. OnEnable, Called when a component of an active GameObject is first enabled.
    private void OnEnable()
    {
    }

    // 3. Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnMoveCameraInput += MoveCamera;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _input.OnCrouchInput += Crouch;
        _input.OnGlideInput += StartGlide;
        _input.OnCancelGlide += CancelGlide;
        _input.OnPunchInput += Punch;
        _cameraManager.OnChangePerspective += ChangePerspective;
    }

    // 4. Update is called every frame, if the MonoBehaviour is enabled.
    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
        CheckIsOnFrontOfClimbingWall();
        CheckStayClimbing();
        Glide();
    }

    // 5. LateUpdate is called every frame, if the Behaviour is enabled.
    private void LateUpdate()
    {
    }

    // 6. FixedUpdate, Update called at regular, fixed intervals as part of Unity's physics update loop.
    private void FixedUpdate()
    {
    }

    // 7. OnDisable, Called when a component itself is disabled or its parent GameObject is deactivated.
    private void OnDisable()
    {
    }

    // 8. OnDestroy, Called when a GameObject or component is about to be destroyed.
    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnMoveCameraInput -= MoveCamera;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimb;
        _input.OnCrouchInput -= Crouch;
        _input.OnGlideInput -= StartGlide;
        _input.OnCancelGlide -= CancelGlide;
        _input.OnPunchInput -= Punch;
        _cameraManager.OnChangePerspective -= ChangePerspective;
    }

    /// Methods
    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Move(Vector2 axisDirection)
    {
        float rotationAngle = 0f;
        Vector3 movementDirection = Vector3.zero;
        Vector3 addForce = Vector3.zero;
        if ((_playerStance == PlayerStance.Stand || _playerStance == PlayerStance.Crouch) && !_isPunching)
        {
            switch (_cameraManager.CameraState)
            {
                case CameraState.ThirdPerson:
                    if (axisDirection.magnitude >= 0.1)
                    {
                        rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        // transform.rotation = Quaternion.Euler(0f, rotationAngle, 0f);
                        movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        addForce = movementDirection * Time.deltaTime * _speed;
                        _rigidbody.AddForce(addForce);
                        // Debug.Log("FreeCameraMove - Rotation: " + rotationAngle + ", Direction: " + movementDirection + ", Speed: " + _speed + ", AddForce: " + addForce);
                    }
                    break;
                case CameraState.FirstPerson:
                    // // Disable body rotation on move, use body rotation on move camera instead
                    // rotationAngle = _cameraTransform.eulerAngles.y;
                    // transform.rotation = Quaternion.Euler(0f, rotationAngle, 0f);
                    Vector3 verticalDirection = axisDirection.y * transform.forward;
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    movementDirection = verticalDirection + horizontalDirection;
                    addForce = movementDirection * Time.deltaTime * _speed;
                    _rigidbody.AddForce(addForce);
                    // Debug.Log("1stPersonMove - Rotation: " + rotationAngle + ", Direction: " + movementDirection + ", Speed: " + _speed + ", AddForce: " + addForce);
                    break;
                default:
                    break;
            }

            Vector3 velocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            _animator.SetFloat("Velocity", velocity.magnitude * axisDirection.magnitude);
            _animator.SetFloat("VelocityZ", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("VelocityX", velocity.magnitude * axisDirection.x);

        }
        else if (_playerStance == PlayerStance.Climb)
        {
            if (_isInFrontOfClimbingWall)
            {
                ClimbMove(axisDirection);
            }
            else
            {
                CancelClimb(); // fall off the ladder
            }
        }
        else if (_playerStance == PlayerStance.Glide)
        {
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += _glideRotationSpeed.x * axisDirection.y * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _minGlideRotationX, _maxGlideRotationX);
            rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
            rotationDegree.y += _glideRotationSpeed.y * axisDirection.x * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationDegree);
        }
    }
    private void MoveCamera(Vector2 axisDirection)
    {
        if (_cameraManager.CameraState == CameraState.FirstPerson)
        {
            if (_playerStance == PlayerStance.Stand || _playerStance == PlayerStance.Crouch)
            {
                float rotationAngleY = transform.eulerAngles.y;
                float rotationMouseX = axisDirection.x * _cameraHorizontalRotationSpeed * Time.deltaTime;
                transform.Rotate(Vector3.up * rotationMouseX);
                // Debug.Log("1stPersonBodyRotation - From: " + rotationAngleY + ", Rotation: " + rotationMouseX + ", RotatedTo: " + Quaternion.Euler(0f, rotationAngleY + rotationMouseX, 0f).eulerAngles.y);
            }
        }
    }

    private void Sprint(bool isSprint)
    {
        if (_playerStance == PlayerStance.Stand)
        {
            if (isSprint)
            {
                if (_speed < _sprintSpeed)
                {
                    _speed = Mathf.Min(_sprintSpeed, _speed + _walkSprintTransition * Time.deltaTime);
                }
            }
            else
            {
                if (_speed > _walkSpeed)
                {
                    _speed = Mathf.Max(_walkSpeed, _speed - _walkSprintTransition * Time.deltaTime);
                }
            }
        }
    }

    private void Jump()
    {
        if (_isGrounded)
        {
            Vector3 jumpDirection = Vector3.up;
            _rigidbody.AddForce(Vector3.up * _jumpForce);
            _animator.SetTrigger("Jump");
        }
    }

    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _groundDetectorRadius, _groundLayer);
        _animator.SetBool("IsGrounded", _isGrounded);
        if (_isGrounded)
        {
            CancelGlide();
        }
    }

    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(_groundDetector.position,
                                                transform.forward,
                                                _stepCheckerDistance);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position +
                                                _upperStepOffset,
                                                transform.forward,
                                                _stepCheckerDistance);
        if (isHitLowerStep && !isHitUpperStep && (_playerStance != PlayerStance.Climb))
        {
            _rigidbody.AddForce(0, _stepForce, 0);
        }
    }

    private void CheckIsOnFrontOfClimbingWall()
    {
        _isInFrontOfClimbingWall = Physics.Raycast(_climbDetector.position,
                                                    transform.forward,
                                                    out RaycastHit raycastHit,
                                                    _climbDetectorDistance,
                                                    _climbableLayer);
        _raycastHit = raycastHit;
        // Debug.Log("ClimbDetector: " + _climbDetector.position + ", RaycastHit:" + _raycastHit.point + ", Dist: " + _raycastHit.distance);
    }

    private void CheckStayClimbing()
    {
        if (_isInFrontOfClimbingWall && (_playerStance == PlayerStance.Climb))
        {
            Vector3 stickToLadderDirection = transform.forward;
            _rigidbody.AddForce(stickToLadderDirection * _climbStickForce);
        }
    }

    private void ClimbMove(Vector2 axisDirection)
    {
        Vector3 horizontal = axisDirection.x * transform.right;
        Vector3 vertical = axisDirection.y * transform.up;
        Vector3 addVelocity = (horizontal + vertical) * _climbSpeed;
        if (_rigidbody.linearVelocity.magnitude + addVelocity.magnitude < _climbVelocityMax)
        {
            _rigidbody.AddForce(addVelocity, ForceMode.VelocityChange);
        }
        // Debug.Log("CurrentVelocity: " + _rigidbody.linearVelocity + " / " + _rigidbody.linearVelocity.magnitude + ", AddVelocity: " + addVelocity + " / " + addVelocity.magnitude);
        Vector3 velocity = new Vector3(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.y, 0);
        _animator.SetFloat("ClimbVelocityX", velocity.magnitude * axisDirection.x);
        _animator.SetFloat("ClimbVelocityY", velocity.magnitude * axisDirection.y);
    }

    private void StartClimb()
    {
        if (_isInFrontOfClimbingWall && _isGrounded && (_playerStance != PlayerStance.Climb))
        {
            _animator.SetBool("IsClimbing", true);
            _playerStance = PlayerStance.Climb;
            Vector3 climbDirection = Vector3.up;
            _rigidbody.AddForce(climbDirection * _climbInitForce);
            // transform.rotation = ; // TODO reset character facing to ladder
            _collider.center = Vector3.up * 1.3f;
            _cameraManager.SetTPSFieldOfView(80);
            _cameraManager.SetFPSCameraLookXInputControllerEnabled(true);
        }
    }

    private void CancelClimb()
    {
        if (_playerStance == PlayerStance.Climb)
        {
            _collider.center = Vector3.up * 0.9f;
            _cameraManager.SetTPSFieldOfView(60);
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsClimbing", false);
            _cameraManager.SetFPSCameraLookXInputControllerEnabled(false);
        }
    }

    private void Crouch()
    {
        if (_playerStance == PlayerStance.Stand)
        {
            _playerStance = PlayerStance.Crouch;
            _animator.SetBool("IsCrouch", true);
            _speed = _crouchSpeed;
            _collider.height = 1.3f;
            _collider.center = Vector3.up * 0.66f;
        }
        else if (_playerStance == PlayerStance.Crouch)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsCrouch", false);
            _speed = _walkSpeed;
            _collider.height = 1.8f;
            _collider.center = Vector3.up * 0.9f;
        }
    }

    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
    }

    private void StartGlide()
    {
        if (_playerStance != PlayerStance.Glide && !_isGrounded)
        {
            _playerStance = PlayerStance.Glide;
            _animator.SetBool("IsGliding", true);
            _cameraManager.SetFPSCameraLookXInputControllerEnabled(true);
        }
    }

    private void CancelGlide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsGliding", false);
            _cameraManager.SetFPSCameraLookXInputControllerEnabled(false);
        }
    }

    private void Glide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 upForce = transform.up * (lift + _airDrag);
            Vector3 forwardForce = transform.forward * _glideSpeed;
            Vector3 totalForce = upForce + forwardForce;
            _rigidbody.AddForce(totalForce * Time.deltaTime);
        }
    }

    private void Punch()
    {
        // Debug.Log("StartPunch! IsPunching: " + _isPunching + ", Combo: " + _combo);
        if (!_isPunching && _playerStance == PlayerStance.Stand)
        {
            _isPunching = true;
            if (_combo < 3)
            {
                _combo = _combo + 1;
            }
            else
            {
                _combo = 1;
            }
            _animator.SetInteger("Combo", _combo);
            _animator.SetTrigger("Punch");
        }
    }

    private void EndPunch()
    {
        _isPunching = false;
        if (_resetCombo != null)
        {
            StopCoroutine(_resetCombo);
        }
        _resetCombo = StartCoroutine(ResetCombo());
        // Debug.Log("EndPunch!!! IsPunching: " + _isPunching + ", Combo: " + _combo);
    }

    private IEnumerator ResetCombo()
    {
        yield return new WaitForSeconds(_resetComboInterval);
        _combo = 0;
    }

    private void Hit()
    {
        // Debug.Log("CheckHit!!");
        Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position,
                                                        _hitDetectorRadius,
                                                        _hitLayer);
        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i].gameObject != null)
            {
                Destroy(hitObjects[i].gameObject);
            }
        }
    }
}
