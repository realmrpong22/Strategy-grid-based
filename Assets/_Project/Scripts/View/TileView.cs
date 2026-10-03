using Tactics.Core;
using UnityEngine;

namespace Tactics.View
{
    /// <summary>
    /// Greybox column for one tile. The root sits at the centre of the tile's top surface, which is
    /// where units will stand; the child column is scaled to fill the space beneath it.
    /// </summary>
    public sealed class TileView : MonoBehaviour
    {
        [Tooltip("Child cube scaled into the column. The root stays unscaled so anything parented to it isn't distorted.")]
        [SerializeField] private Transform _column;
        [SerializeField] private MeshRenderer _renderer;

        [Header("Highlight")]
        [Tooltip("Child quad lying just above the top surface.")]
        [SerializeField] private Transform _highlight;
        [SerializeField] private MeshRenderer _highlightRenderer;
        [SerializeField] private Material _hoverMaterial;
        [SerializeField] private Material _selectedMaterial;

        public Tile Tile { get; private set; }
        public Vector2Int Position => Tile.Position;

        internal void Initialize(Tile tile, GridMetrics metrics, float footprint, Material material)
        {
            Tile = tile;

            // Runs from one step below ground up to the top surface, so height-0 tiles are a slab, not a plane.
            float columnHeight = (tile.Height + 1) * metrics.HeightStep;
            float width = metrics.CellSize * footprint;
            _column.localScale = new Vector3(width, columnHeight, width);
            _column.localPosition = new Vector3(0f, -columnHeight * 0.5f, 0f);

            // sharedMaterial: .material would silently clone a material per tile.
            _renderer.sharedMaterial = material;

            // The quad is rotated 90° on X, so its local X/Y map to world X/Z.
            _highlight.localScale = new Vector3(width, width, 1f);
            SetHighlight(TileHighlight.None);
        }

        public void SetHighlight(TileHighlight highlight)
        {
            Material material = highlight switch
            {
                TileHighlight.Hover => _hoverMaterial,
                TileHighlight.Selected => _selectedMaterial,
                _ => null,
            };

            // Disable rather than deactivate: cheaper, and the GameObject stays in place for later states.
            _highlightRenderer.enabled = material != null;
            if (material != null)
                _highlightRenderer.sharedMaterial = material;
        }
    }
}
