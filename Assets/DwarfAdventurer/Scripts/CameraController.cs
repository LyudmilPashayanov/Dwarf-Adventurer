using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private const float RAY_DISTANCE = 13;

    [SerializeField] private Transform _cameraPivot;
    [SerializeField] private Transform _camera;
    [SerializeField] private Transform _cameraParent;
    [SerializeField] private Transform _player;
    [SerializeField] private Collider _playerCollider;
   

    private Quaternion _cameraGoToPivotRotation;
    private bool _applyLocal=true;
    private bool _unlockedViewPlayer;
    private bool _unlockedView;
    private float _MouseRotationDifferenceX;
    private float _MouseRotationDifferenceY;
    private Vector3 _defaultDistance;
    private Vector3 _cameraParentGoToPosition;
    private Vector3 _cameraParentGoToLocalPosition;
    private bool _cameraIsFreed;
    
    [Range(2, 4)] private float _sensitivityX = 200;
    [Range(2, 4)] private float _sensitivityY = 150;

    private void Start()
    {
        _defaultDistance = _cameraParent.localPosition;
    }

    private void Update()
    {
        GatherInput();
        CalculateMouseMovement();
        CalculateLineOfSight();
    }

    private void FixedUpdate()
    {
        ApplyCameraMovement();
    }

    private void ApplyCameraMovement()
    {
        if (_applyLocal)
        {
            _cameraParent.localPosition = Vector3.Lerp(_cameraParent.localPosition, _cameraParentGoToLocalPosition, 10f * Time.deltaTime);
        }
        else
        {
            _cameraParent.position = Vector3.Lerp(_cameraParent.position, _cameraParentGoToPosition, 10f * Time.deltaTime);
        }
        
        _cameraPivot.rotation = Quaternion.Lerp(_cameraPivot.rotation, _cameraGoToPivotRotation, 10f * Time.deltaTime);
        _cameraPivot.position = Vector3.Lerp(_cameraPivot.position, _player.position, 10f * Time.deltaTime);
        if (_unlockedViewPlayer)
        {
            _player.forward = new Vector3(_cameraPivot.forward.x, 0, _cameraPivot.forward.z);
        }
    }

    private void CalculateMouseMovement()
    {
        if (_unlockedViewPlayer)
        {
            _MouseRotationDifferenceX += Input.GetAxis("Mouse X") * _sensitivityX * Time.deltaTime;
            _MouseRotationDifferenceY -= Input.GetAxis("Mouse Y") * _sensitivityY * Time.deltaTime;
            _cameraGoToPivotRotation = Quaternion.Euler(_MouseRotationDifferenceY, _MouseRotationDifferenceX, _cameraPivot.rotation.z);
        }

        if (_unlockedView)
        {
            _MouseRotationDifferenceX += Input.GetAxis("Mouse X") * _sensitivityX * Time.deltaTime;
            _MouseRotationDifferenceY -= Input.GetAxis("Mouse Y") * _sensitivityY * Time.deltaTime;
            _cameraGoToPivotRotation = Quaternion.Euler(_MouseRotationDifferenceY, _MouseRotationDifferenceX, _cameraPivot.rotation.z);
        }
    }

    
    private void CalculateLineOfSight()
    {
        RaycastHit PlayerRayHit;
        if (Physics.Raycast(new Ray(_playerCollider.bounds.center, _cameraParent.position - _playerCollider.bounds.center), out PlayerRayHit, RAY_DISTANCE))
        {
            _cameraIsFreed = false;
             //_cameraParent.position = PlayerRayHit.point;
             _applyLocal = false;
            _cameraParentGoToPosition = PlayerRayHit.point;
        }
        else
        {
            if (_cameraIsFreed) return;
            // _cameraParent.localPosition = _defaultDistance;
            _applyLocal = true;
            _cameraParentGoToLocalPosition = _defaultDistance;
            _cameraIsFreed = true;
        }
    }
    
    private void GatherInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            _unlockedViewPlayer = true;
        }

        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            _unlockedViewPlayer = false;
        }
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            _unlockedView = true;
        }

        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            _unlockedView = false;
        }
    }
}
