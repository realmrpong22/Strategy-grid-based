using System;
using System.Collections.Generic;
using Tactics.Core;
using UnityEngine;

namespace Tactics.View
{
    /// <summary>
    /// Owns "what is selected" and turns tile clicks into commands:
    /// click a player unit → show its move range; click a tile in range → move there;
    /// click an enemy → show its range (read-only); click the selected unit, an empty tile or Cancel → deselect.
    /// Raises events for display; TileHighlighter draws them. Core is updated first, then the view animates.
    /// </summary>
    public sealed class SelectionController : MonoBehaviour
    {
        // Until turns exist, the player controls this faction and only inspects the others.
        private const Faction PlayerFaction = Faction.Player;

        private enum State
        {
            Idle,
            UnitSelected,
            Moving,
        }

        [SerializeField] private GridPointer _pointer;
        [SerializeField] private UnitsView _unitsView;

        private static readonly Vector2Int[] NoTiles = Array.Empty<Vector2Int>();

        private readonly List<Vector2Int> _path = new List<Vector2Int>();
        private BattleState _battle;
        private State _state;
        private MoveRange _range;

        /// <summary>Tile of the selected unit, or null when nothing is selected.</summary>
        public event Action<Vector2Int?> SelectedTileChanged;
        /// <summary>Tiles to show as a range and how; an empty list clears it.</summary>
        public event Action<IReadOnlyList<Vector2Int>, TileHighlight> RangeChanged;

        public void Initialize(BattleState battle)
        {
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            Deselect();
        }

        private void OnEnable()
        {
            _pointer.TileSelected += OnTileSelected;
            _pointer.Cancelled += OnCancelled;
        }

        private void OnDisable()
        {
            _pointer.TileSelected -= OnTileSelected;
            _pointer.Cancelled -= OnCancelled;
        }

        private void OnTileSelected(TileView tile)
        {
            if (_battle == null || _state == State.Moving)
                return;

            Vector2Int position = tile.Position;

            if (_battle.TryGetUnitAt(position, out Unit unit))
            {
                if (_range != null && unit == _range.Unit)
                    Deselect();
                else
                    Select(unit);
                return;
            }

            if (_range != null && IsControllable(_range.Unit) && _range.CanMoveTo(position))
            {
                Move(_range.Unit, position);
                return;
            }

            Deselect();
        }

        private void OnCancelled()
        {
            if (_state != State.Moving)
                Deselect();
        }

        private static bool IsControllable(Unit unit) => unit.Faction == PlayerFaction;

        private void Select(Unit unit)
        {
            // Recomputed on every selection: a MoveRange is a snapshot and goes stale once anything moves.
            _range = Pathfinder.ComputeMoveRange(_battle, unit);
            _state = State.UnitSelected;
            SelectedTileChanged?.Invoke(unit.Position);
            RangeChanged?.Invoke(_range.Destinations, IsControllable(unit) ? TileHighlight.MoveRange : TileHighlight.EnemyRange);
        }

        private void Deselect()
        {
            _range = null;
            _state = State.Idle;
            SelectedTileChanged?.Invoke(null);
            RangeChanged?.Invoke(NoTiles, TileHighlight.None);
        }

        private void Move(Unit unit, Vector2Int destination)
        {
            _range.TryGetPath(destination, _path);
            var waypoints = new List<Vector3>(_path.Count);
            foreach (Vector2Int step in _path)
                waypoints.Add(_unitsView.TileTopWorld(_battle.Map, step));

            // Core first: the battle state is the truth, the walk just catches the visuals up.
            _battle.MoveUnit(unit, destination);

            Deselect();
            _state = State.Moving;

            if (_unitsView.TryGetView(unit, out UnitView view))
                view.WalkPath(waypoints, OnMoveFinished);
            else
                OnMoveFinished();
        }

        private void OnMoveFinished() => _state = State.Idle;
    }
}
