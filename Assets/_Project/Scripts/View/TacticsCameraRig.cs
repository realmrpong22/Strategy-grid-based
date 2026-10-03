using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tactics.View
{
    /// <summary>
    /// Lives on the camera pivot: a ground-level transform the CinemachineCamera follows and looks at.
    /// This script only moves/rotates the pivot and sets the follow distance; Cinemachine positions the
    /// actual Camera. Pan is relative to the current view, rotation snaps in 90° steps, zoom moves along
    /// a fixed pitch.
    /// </summary>
    public sealed class TacticsCameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _virtualCamera;
        [Tooltip("CinemachineFollow on the virtual camera; Binding Mode must be Lock To Target With World Up.")]
        [SerializeField] private CinemachineFollow _follow;
        [SerializeField] private GridView _gridView;

        [Header("Input")]
        [SerializeField] private InputActionReference _panAction;
        [SerializeField] private InputActionReference _rotateAction;
        [SerializeField] private InputActionReference _zoomAction;

        [Header("Pan")]
        [Tooltip("World units per second.")]
        [SerializeField, Min(0f)] private float _panSpeed = 8f;

        [Header("Rotate")]
        [SerializeField] private float _rotateStep = 90f;
        [Tooltip("Degrees per second while turning to the next step.")]
        [SerializeField, Min(1f)] private float _rotateSpeed = 360f;

        [Header("Zoom")]
        [Tooltip("Camera angle above the ground, in degrees.")]
        [SerializeField, Range(10f, 89f)] private float _pitch = 50f;
        [SerializeField, Min(1f)] private float _minDistance = 6f;
        [SerializeField, Min(1f)] private float _maxDistance = 22f;
        [SerializeField, Min(1f)] private float _startDistance = 14f;
        [Tooltip("Distance change per scroll notch.")]
        [SerializeField, Min(0.1f)] private float _zoomStep = 2f;
        [Tooltip("Higher = snappier zoom. Frame-rate independent.")]
        [SerializeField, Min(0.1f)] private float _zoomSharpness = 12f;

        private float _targetYaw;
        private float _currentYaw;
        private float _targetDistance;
        private float _currentDistance;

        private void Awake()
        {
            _currentYaw = _targetYaw = transform.eulerAngles.y;
            _currentDistance = _targetDistance = Mathf.Clamp(_startDistance, _minDistance, _maxDistance);
            ApplyRig();
        }

        // Actions are enabled but never disabled here: they're shared through the TacticsInput asset.
        private void OnEnable()
        {
            _panAction.action.Enable();
            _rotateAction.action.performed += OnRotatePerformed;
            _rotateAction.action.Enable();
            _zoomAction.action.performed += OnZoomPerformed;
            _zoomAction.action.Enable();

            _gridView.Built += OnGridBuilt;
            if (_gridView.Map != null)
                OnGridBuilt();
        }

        private void OnDisable()
        {
            _rotateAction.action.performed -= OnRotatePerformed;
            _zoomAction.action.performed -= OnZoomPerformed;
            _gridView.Built -= OnGridBuilt;
        }

        // Continuous movement and smoothing need a per-frame update; discrete input (rotate/zoom) is event-driven.
        private void Update()
        {
            float deltaTime = Time.deltaTime;

            Vector2 pan = _panAction.action.ReadValue<Vector2>();
            if (pan != Vector2.zero)
            {
                // The pivot only ever yaws, so its forward/right are flat and match the view.
                Vector3 move = (transform.right * pan.x + transform.forward * pan.y) * (_panSpeed * deltaTime);
                transform.position = _gridView.ClampToGrid(transform.position + move);
            }

            _currentYaw = Mathf.MoveTowardsAngle(_currentYaw, _targetYaw, _rotateSpeed * deltaTime);
            // 1 - exp(-k·dt) instead of a fixed lerp factor, so the zoom feels the same at any frame rate.
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, 1f - Mathf.Exp(-_zoomSharpness * deltaTime));
            ApplyRig();
        }

        private void ApplyRig()
        {
            transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
            // Behind and above the pivot, in the pivot's yaw frame.
            _follow.FollowOffset = Quaternion.Euler(_pitch, 0f, 0f) * new Vector3(0f, 0f, -_currentDistance);
        }

        private void OnRotatePerformed(InputAction.CallbackContext context)
        {
            float direction = context.ReadValue<float>();
            if (direction > 0.5f)
                _targetYaw += _rotateStep;
            else if (direction < -0.5f)
                _targetYaw -= _rotateStep;

            _targetYaw = Mathf.Repeat(_targetYaw, 360f);
        }

        // Scroll magnitude differs by platform and Input System settings (±1 vs ±120 per notch),
        // so only the sign is used: one notch = one zoom step.
        private void OnZoomPerformed(InputAction.CallbackContext context)
        {
            float scroll = context.ReadValue<float>();
            if (Mathf.Approximately(scroll, 0f))
                return;

            _targetDistance = Mathf.Clamp(_targetDistance - Mathf.Sign(scroll) * _zoomStep, _minDistance, _maxDistance);
        }

        private void OnGridBuilt()
        {
            transform.position = _gridView.CenterWorld;
            // Snap instead of damping in from wherever the camera was before the map existed.
            _virtualCamera.PreviousStateIsValid = false;
        }
    }
}
