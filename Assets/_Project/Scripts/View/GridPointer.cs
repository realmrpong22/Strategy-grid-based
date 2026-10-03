using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tactics.View
{
    /// <summary>
    /// Turns pointer input into tile events. Knows nothing about what hovering or selecting means;
    /// listeners decide that. Works for mouse now and touch later, since it only reads input actions.
    /// </summary>
    public sealed class GridPointer : MonoBehaviour
    {
        [Tooltip("The Unity Camera to raycast from. With Cinemachine this is still the Main Camera, not the CinemachineCamera.")]
        [SerializeField] private Camera _camera;
        [SerializeField] private InputActionReference _pointAction;
        [SerializeField] private InputActionReference _selectAction;
        [SerializeField] private InputActionReference _cancelAction;
        [Tooltip("Layers the tile colliders are on.")]
        [SerializeField] private LayerMask _tileLayers;
        [SerializeField, Min(1f)] private float _maxRayDistance = 200f;

        /// <summary>Raised when the tile under the pointer changes; null when it leaves the grid.</summary>
        public event Action<TileView> HoveredTileChanged;
        public event Action<TileView> TileSelected;
        public event Action Cancelled;

        public TileView HoveredTile { get; private set; }

        // Actions are enabled here but never disabled: other components may share them,
        // and disabling an action disables it for every listener.
        private void OnEnable()
        {
            _pointAction.action.Enable();
            _selectAction.action.performed += OnSelectPerformed;
            _selectAction.action.Enable();
            _cancelAction.action.performed += OnCancelPerformed;
            _cancelAction.action.Enable();
        }

        private void OnDisable()
        {
            _selectAction.action.performed -= OnSelectPerformed;
            _cancelAction.action.performed -= OnCancelPerformed;
        }

        // Polled on purpose: the tile under a still pointer changes whenever the camera moves, and no
        // input event fires for that. Listeners still only hear about actual changes.
        private void Update()
        {
            TileView tile = RaycastTile();
            if (tile == HoveredTile)
                return;

            HoveredTile = tile;
            HoveredTileChanged?.Invoke(tile);
        }

        private void OnSelectPerformed(InputAction.CallbackContext context)
        {
            // Fresh raycast rather than HoveredTile, which is from last frame's Update.
            TileView tile = RaycastTile();
            if (tile != null)
                TileSelected?.Invoke(tile);
        }

        private void OnCancelPerformed(InputAction.CallbackContext context) => Cancelled?.Invoke();

        private TileView RaycastTile()
        {
            Vector2 screenPosition = _pointAction.action.ReadValue<Vector2>();
            Ray ray = _camera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _tileLayers, QueryTriggerInteraction.Ignore))
                return null;

            // The collider is on the Column child; the TileView is on the root.
            return hit.collider.GetComponentInParent<TileView>();
        }
    }
}
