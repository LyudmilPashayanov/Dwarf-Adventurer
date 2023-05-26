using System;
using UnityEngine;

namespace TowerDefense.Character
{
    public struct FrameInput
    {
        public float X;
        public float Y;
        public bool JumpDown;
        public bool JumpUp;
    }

    public class PlayerMovement : MonoBehaviour
    {
        public Action<Vector3> OnPlayerMove;

        private FrameInput Input;

        // private float _lastJumpPressed;    Jumping code
        private float _currentXSpeed, _gravitySpeed, _currentZSpeed;
        [Header("WALKING")] [SerializeField] private float _acceleration = 90;
        [SerializeField] private float _moveClamp = 13;
        [SerializeField] private float _deAcceleration = 60f;
        [SerializeField] private float _apexBonus = 2;
        [Range(0.1f, 0.5f)] [SerializeField] private float _raycastSize = 0.2f;

        [SerializeField] private MeshFilter _playerMesh;

        private Vector3 RawMovement;
        private Rigidbody _rigidbody;
        private RaycastHit _rayGroundHit;

        private void Awake()
        {
            _rigidbody = transform.GetComponent<Rigidbody>();
            _playerMesh = gameObject.GetComponent<MeshFilter>();
        }

        void Update()
        {
            GatherInput();
            CalculateWalk();
            CalculateGravity();
            CalculateJumpApex();
            CalculateJump();
        }

        private void FixedUpdate()
        {
            CalculateOnGroundCheck();
            MoveCharacter();
        }

        private void GatherInput()
        {
            Input = new FrameInput
            {
                JumpDown = UnityEngine.Input.GetKeyDown(KeyCode.Space),
                JumpUp = UnityEngine.Input.GetKeyUp(KeyCode.Space),
                X = UnityEngine.Input.GetAxisRaw("Horizontal"),
                Y = UnityEngine.Input.GetAxisRaw("Vertical")
            };
            if (Input.JumpDown)
            {
                _lastJumpPressed = Time.time;
            }

            // End the jump early if button released
            if (Input.JumpUp && !_onGround && !_endedJumpEarly && _rigidbody.velocity.y > 0)
            {
                _endedJumpEarly = true;
            }
        }

        private void CalculateOnGroundCheck()
        {
            if (Physics.Raycast(new Ray(transform.position, transform.TransformDirection(Vector3.down)),
                    out _rayGroundHit, _playerMesh.mesh.bounds.size.y / 2 + _raycastSize))
            {
                if (_rayGroundHit.collider.gameObject.name ==
                    "Terrain") // TODO: don't check the name of the terrain but use layer or other tagging mechanism
                {
                    Debug.DrawRay(transform.position,
                        transform.TransformDirection(Vector3.down) * _rayGroundHit.distance, Color.red);
                    if (!_onGround)
                        _coyoteUsable = true; // Only trigger when first touching

                    _onGround = true;
                    return;
                }
            }

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * _rayGroundHit.distance,
                Color.yellow);
            if (_onGround)
                _timeLeftGrounded = Time.time;
            _onGround = false;
        }

        private void MoveCharacter()
        {
            RawMovement = new Vector3(_currentXSpeed, _gravitySpeed, _currentZSpeed);
            var move = RawMovement * Time.deltaTime;
            _rigidbody.velocity = transform.TransformVector(move);
            OnPlayerMove?.Invoke(move);
        }

        private void CalculateWalk()
        {
            if (Input.X != 0)
            {
                // Set horizontal move speed
                _currentXSpeed += Input.X * _acceleration * Time.deltaTime;

                // clamped by max frame movement
                _currentXSpeed = Mathf.Clamp(_currentXSpeed, -_moveClamp, _moveClamp);

                // Apply bonus at the apex of a jump
                var apexBonus = Mathf.Sign(Input.X) * _apexBonus * _apexPoint;
                _currentXSpeed += apexBonus * Time.deltaTime;
            }
            else
            {
                // No input. Let's slow the character down
                _currentXSpeed = Mathf.MoveTowards(_currentXSpeed, 0, _deAcceleration * Time.deltaTime);
            }

            if (Input.Y != 0)
            {
                // Set horizontal move speed
                _currentZSpeed += Input.Y * _acceleration * Time.deltaTime;

                // clamped by max frame movement
                _currentZSpeed = Mathf.Clamp(_currentZSpeed, -_moveClamp, _moveClamp);

                // Apply bonus at the apex of a jump
                var apexBonus = Mathf.Sign(Input.X) * _apexBonus * _apexPoint;
                _currentZSpeed += apexBonus * Time.deltaTime;
            }
            else
            {
                // No input. Let's slow the character down
                _currentZSpeed = Mathf.MoveTowards(_currentZSpeed, 0, _deAcceleration * Time.deltaTime);
            }
        }

        #region Jump&Gravity

        [Header("GRAVITY")] [SerializeField] private float _fallClamp = -700f;

        [SerializeField]
        private float _minFallSpeed = 2000f; // If different than _maxFallSpeed you have accelerated jump and falling

        [SerializeField]
        private float _maxFallSpeed = 3000f; // If different than _minFallSpeed you have accelerated jump and falling

        [SerializeField]
        private float _jumpEndEarlyGravityModifier = 1; // If more you can end your jump earlier when you release "jump"

        [Header("JUMPING")] [SerializeField] private float _jumpSpeed = 900;
        [SerializeField] private float _jumpApexThreshold = 500f;
        [SerializeField] private float _coyoteTimeThreshold = 0.15f;
        [SerializeField] private float _jumpBuffer = 0.15f;
        private float _fallSpeed;
        private float _apexPoint; // Becomes 1 at the apex of a jump
        private float _lastJumpPressed;

        private bool _onGround;
        private float _timeLeftGrounded;

        private bool _endedJumpEarly;
        private bool _coyoteUsable;

        private bool CanUseCoyote =>
            _coyoteUsable && !_onGround && _timeLeftGrounded + _coyoteTimeThreshold > Time.time;

        private bool HasBufferedJump =>
            _onGround && _lastJumpPressed + _jumpBuffer > Time.time; // Allows you to jump before you've hit the ground.

        private bool JumpingThisFrame;

        private void CalculateGravity()
        {
            if (_onGround)
            {
                // Move out of the ground
                if (_gravitySpeed < 0) _gravitySpeed = 0;
            }
            else
            {
                // Add downward force while ascending if we ended the jump early
                var fallSpeed = _endedJumpEarly && _gravitySpeed > 0
                    ? _fallSpeed * _jumpEndEarlyGravityModifier
                    : _fallSpeed;
                _gravitySpeed -= fallSpeed * Time.deltaTime;

                if (_gravitySpeed < _fallClamp)
                {
                    _gravitySpeed = _fallClamp; // Clamping
                }
            }
        }

        private void CalculateJumpApex()
        {
            if (!_onGround)
            {
                // Gets stronger the closer to the top of the jump
                _apexPoint = Mathf.InverseLerp(_jumpApexThreshold, 0, Mathf.Abs(_rigidbody.velocity.y));
                _fallSpeed = Mathf.Lerp(_minFallSpeed, _maxFallSpeed, _apexPoint);
            }
            else
            {
                _apexPoint = 0;
            }
        }

        private void CalculateJump()
        {
            // Jump if: grounded or within coyote threshold || sufficient jump buffer
            if (Input.JumpDown && CanUseCoyote || HasBufferedJump)
            {
                _gravitySpeed = _jumpSpeed;
                _endedJumpEarly = false;
                _coyoteUsable = false;
                _timeLeftGrounded = float.MinValue;
                JumpingThisFrame = true;
            }
            else
            {
                JumpingThisFrame = false;
            }

            if (false /*if head hits the ceiling*/)
            {
                if (_gravitySpeed > 0)
                    _gravitySpeed = 0;
            }
        }

        #endregion
    }
}