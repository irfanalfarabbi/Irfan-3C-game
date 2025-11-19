using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private InputManager _input;
    private Rigidbody _rigidbody;
    private PlayerStance _playerStance;

    // walking, running, upstep
    [SerializeField]
    private float _walkSpeed;
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _walkSprintTransition;
    [SerializeField]
    private Vector3 _upperStepOffset;
    [SerializeField]
    private float _stepCheckerDistance;
    [SerializeField]
    private float _stepForce;
    private float _speed;
    [SerializeField]
    private float _rotationSmoothTime = 0.1f;
    private float _rotationSmoothVelocity;

    // jumping
    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private LayerMask _groundLayer;
    [SerializeField]
    private float _detectorRadius;
    [SerializeField]
    private float _jumpForce;
    private bool _isGrounded;

    // climbing
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private LayerMask _climbableLayer;
    [SerializeField]
    private float _climbCheckDistance;
    [SerializeField]
    private float _climbSpeed;
    [SerializeField]
    private float _climbSpeedUp;
    [SerializeField]
    private float _climbSpeedDown;
    [SerializeField]
    private float _climbSpeedSide;
    [SerializeField]
    private float _climbStickForce;
    private bool _isClimbStep;
    private float _climbTimer;
    [SerializeField]
    private float _climbDelaySmoothTime = 0.1f;
    private bool _isInFrontOfClimbingWall;
    private RaycastHit _raycastHit;

    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;

    }
    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
        CheckIsOnFrontOfClimbingWall();
        CheckStayClimbing();
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _speed = _walkSpeed;
        _playerStance = PlayerStance.Stand;
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimb;
    }


    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        if (isPlayerStanding)
        {
            if (axisDirection.magnitude >= 0.1)
            {
                float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                _rigidbody.AddForce(movementDirection * Time.deltaTime * _speed);
            }
        }
        else if (isPlayerClimbing)
        {
            if (_isInFrontOfClimbingWall)
            {
                _climbTimer += Time.deltaTime;
                if (!_isClimbStep)
                {
                    float climbSpeedFactor = _climbSpeedUp;
                    float vertical = 1f;
                    float horizontal = 1f;
                    if (axisDirection.y != 0)
                    {
                        if (axisDirection.y < 0)
                        {
                            vertical = -1f;
                            climbSpeedFactor *= _climbSpeedDown;
                        }
                        movementDirection += transform.up * vertical;
                    }
                    if (axisDirection.x != 0)
                    {
                        if (axisDirection.y == 0)
                        {
                            movementDirection += transform.up * 0.2f;
                        }
                        else
                        {
                            climbSpeedFactor *= 0.8f;
                        }
                        if (axisDirection.x < 0)
                        {
                            horizontal = -1f;
                        }
                        movementDirection += transform.right * horizontal;
                        climbSpeedFactor *= _climbSpeedSide;
                    }
                    _rigidbody.AddForce(movementDirection * _climbSpeed * climbSpeedFactor);
                    _isClimbStep = true;
                }
                else if (_climbTimer >= _climbDelaySmoothTime)
                {
                    _climbTimer = 0f;
                    _isClimbStep = false;
                }
            }
            else
            {
                CancelClimb(); // fall off the ladder
            }
        }
    }

    private void Sprint(bool isSprint)
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


    private void Jump()
    {
        if (_isGrounded)
        {
            Vector3 jumpDirection = Vector3.up;
            _rigidbody.AddForce(Vector3.up * _jumpForce);
        }
    }

    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
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
                                                            _climbCheckDistance,
                                                            _climbableLayer);
        _raycastHit = raycastHit;
    }

    private void CheckStayClimbing()
    {
        if (_isInFrontOfClimbingWall && (_playerStance == PlayerStance.Climb))
        {
            Vector3 stickToLadderDirection = transform.forward;
            _rigidbody.AddForce(stickToLadderDirection * _climbStickForce);
        }
    }

    private void StartClimb()
    {
        if (_isInFrontOfClimbingWall && _isGrounded && (_playerStance != PlayerStance.Climb))
        {
            Debug.Log("Char.Pos: " + _climbDetector.position + ", RaycastHit:" + _raycastHit.point + ", Dist: " + _raycastHit.distance);
            Vector3 climbDirection = Vector3.up;
            _playerStance = PlayerStance.Climb;
            _rigidbody.AddForce(climbDirection * _climbSpeed);
            // transform.rotation = ; // TODO reset character facing to ladder
        }
    }

    private void CancelClimb()
    {
        if (_playerStance == PlayerStance.Climb)
        {
            _playerStance = PlayerStance.Stand;
            _climbTimer = 0f;
            _isClimbStep = false;
        }
    }
}
