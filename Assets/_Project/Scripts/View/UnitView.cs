using System;
using System.Collections;
using System.Collections.Generic;
using Tactics.Core;
using UnityEngine;

namespace Tactics.View
{
    /// <summary>
    /// Greybox body for one Core unit. The root sits at the unit's feet (the tile's top surface) and
    /// stays unscaled; children carry the visuals. Reads the Unit, never changes it.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        [Tooltip("Capsule tinted with the unit class colour.")]
        [SerializeField] private MeshRenderer _bodyRenderer;
        [Tooltip("Small shape above the body tinted with the faction colour, readable from any camera angle.")]
        [SerializeField] private MeshRenderer _factionMarkerRenderer;

        [Header("Walking (presentation only)")]
        [SerializeField, Min(0.1f)] private float _tilesPerSecond = 6f;
        [Tooltip("Extra height above the higher tile's edge when hopping between heights, so feet clear the corner.")]
        [SerializeField, Min(0f)] private float _hopClearance = 0.15f;

        private Coroutine _walk;

        public Unit Unit { get; private set; }

        internal void Initialize(Unit unit, Material bodyMaterial, Material factionMaterial)
        {
            Unit = unit;
            // sharedMaterial: .material would clone a material per unit.
            _bodyRenderer.sharedMaterial = bodyMaterial;
            _factionMarkerRenderer.sharedMaterial = factionMaterial;
        }

        /// <summary>Puts the unit's feet at a world position (a tile's top-surface centre).</summary>
        public void PlaceAt(Vector3 worldPosition) => transform.position = worldPosition;

        /// <summary>
        /// Walks through world waypoints (tile top centres, first = current tile), one tile per segment,
        /// then calls <paramref name="onComplete"/>. Starting a walk cancels one in progress without calling
        /// its callback; SelectionController never does that, since it ignores input while a unit walks.
        /// </summary>
        public void WalkPath(IReadOnlyList<Vector3> waypoints, Action onComplete)
        {
            if (waypoints == null) throw new ArgumentNullException(nameof(waypoints));

            if (_walk != null)
                StopCoroutine(_walk);
            _walk = StartCoroutine(Walk(waypoints, onComplete));
        }

        private IEnumerator Walk(IReadOnlyList<Vector3> waypoints, Action onComplete)
        {
            float segmentDuration = 1f / _tilesPerSecond;

            for (int i = 1; i < waypoints.Count; i++)
            {
                Vector3 from = waypoints[i - 1];
                Vector3 to = waypoints[i];
                Face(to - from);

                // Linear interpolation between different heights would cut through the higher tile's
                // corner (at the border, feet sit half a height difference too low). An arc peaking at
                // half the difference plus clearance keeps the feet above the edge in both directions.
                float heightDifference = Mathf.Abs(to.y - from.y);
                float arc = heightDifference > 0.001f ? heightDifference * 0.5f + _hopClearance : 0f;

                for (float elapsed = 0f; elapsed < segmentDuration; elapsed += Time.deltaTime)
                {
                    float t = elapsed / segmentDuration;
                    Vector3 position = Vector3.Lerp(from, to, t);
                    position.y += arc * 4f * t * (1f - t);
                    transform.position = position;
                    yield return null;
                }

                transform.position = to;
            }

            _walk = null;
            onComplete?.Invoke();
        }

        // Yaw only, so a hop never tilts the unit. Matters once Mixamo models replace the capsule.
        private void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
