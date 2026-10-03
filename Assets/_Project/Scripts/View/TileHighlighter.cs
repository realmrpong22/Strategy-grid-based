using UnityEngine;

namespace Tactics.View
{
    /// <summary>
    /// Shows hover and selection on tiles by listening to GridPointer.
    /// Selection lives here only for Week 1; in Week 2 a battle controller owns "what is selected"
    /// and this class just displays it.
    /// </summary>
    public sealed class TileHighlighter : MonoBehaviour
    {
        [SerializeField] private GridPointer _pointer;

        private TileView _hovered;
        private TileView _selected;

        private void OnEnable()
        {
            _pointer.HoveredTileChanged += OnHoveredTileChanged;
            _pointer.TileSelected += OnTileSelected;
            _pointer.Cancelled += OnCancelled;
        }

        private void OnDisable()
        {
            _pointer.HoveredTileChanged -= OnHoveredTileChanged;
            _pointer.TileSelected -= OnTileSelected;
            _pointer.Cancelled -= OnCancelled;
        }

        private void OnHoveredTileChanged(TileView tile)
        {
            TileView previous = _hovered;
            _hovered = tile;
            Refresh(previous);
            Refresh(_hovered);
        }

        // Clicking the selected tile again deselects it.
        private void OnTileSelected(TileView tile)
        {
            TileView previous = _selected;
            _selected = tile == _selected ? null : tile;
            Refresh(previous);
            Refresh(tile);
        }

        private void OnCancelled()
        {
            TileView previous = _selected;
            _selected = null;
            Refresh(previous);
        }

        /// <summary>Selected wins over hover, so the selection stays visible under the pointer.</summary>
        private void Refresh(TileView tile)
        {
            if (tile == null)
                return;

            if (tile == _selected)
                tile.SetHighlight(TileHighlight.Selected);
            else if (tile == _hovered)
                tile.SetHighlight(TileHighlight.Hover);
            else
                tile.SetHighlight(TileHighlight.None);
        }
    }
}
