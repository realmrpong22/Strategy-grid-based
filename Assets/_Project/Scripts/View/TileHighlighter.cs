using System.Collections.Generic;
using UnityEngine;

namespace Tactics.View
{
    /// <summary>
    /// Display only: draws hover (from GridPointer) plus the selected tile and range (from SelectionController).
    /// Priority per tile: Selected, then Hover, then range.
    /// </summary>
    public sealed class TileHighlighter : MonoBehaviour
    {
        [SerializeField] private GridPointer _pointer;
        [SerializeField] private SelectionController _selection;
        [SerializeField] private GridView _gridView;

        private TileView _hovered;
        private TileView _selected;
        private HashSet<TileView> _range = new HashSet<TileView>();
        private HashSet<TileView> _previousRange = new HashSet<TileView>();
        private TileHighlight _rangeHighlight;

        private void OnEnable()
        {
            _pointer.HoveredTileChanged += OnHoveredTileChanged;
            _selection.SelectedTileChanged += OnSelectedTileChanged;
            _selection.RangeChanged += OnRangeChanged;
        }

        private void OnDisable()
        {
            _pointer.HoveredTileChanged -= OnHoveredTileChanged;
            _selection.SelectedTileChanged -= OnSelectedTileChanged;
            _selection.RangeChanged -= OnRangeChanged;
        }

        private void OnHoveredTileChanged(TileView tile)
        {
            TileView previous = _hovered;
            _hovered = tile;
            Refresh(previous);
            Refresh(_hovered);
        }

        private void OnSelectedTileChanged(Vector2Int? position)
        {
            TileView previous = _selected;
            _selected = position.HasValue && _gridView.TryGetTileView(position.Value, out TileView view) ? view : null;
            Refresh(previous);
            Refresh(_selected);
        }

        private void OnRangeChanged(IReadOnlyList<Vector2Int> tiles, TileHighlight highlight)
        {
            // Swap sets so the old range can be refreshed (cleared) without allocating.
            (_previousRange, _range) = (_range, _previousRange);
            _range.Clear();
            _rangeHighlight = highlight;

            foreach (Vector2Int position in tiles)
            {
                if (_gridView.TryGetTileView(position, out TileView view))
                    _range.Add(view);
            }

            foreach (TileView tile in _previousRange)
                Refresh(tile);
            foreach (TileView tile in _range)
                Refresh(tile);
            _previousRange.Clear();
        }

        private void Refresh(TileView tile)
        {
            if (tile == null)
                return;

            if (tile == _selected)
                tile.SetHighlight(TileHighlight.Selected);
            else if (tile == _hovered)
                tile.SetHighlight(TileHighlight.Hover);
            else if (_range.Contains(tile))
                tile.SetHighlight(_rangeHighlight);
            else
                tile.SetHighlight(TileHighlight.None);
        }
    }
}
