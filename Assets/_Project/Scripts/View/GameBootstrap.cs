using Tactics.Core;
using Tactics.Data;
using UnityEngine;

namespace Tactics.View
{
    /// <summary>
    /// Scene composition root: builds Core state from data and hands it to the views.
    /// The one allowed scene-level manager (see CLAUDE.md). It is not a singleton; nothing looks it up.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private MapData _mapData;
        [SerializeField] private GridView _gridView;

        // Start, not Awake: every view has finished its own Awake setup by now.
        private void Start()
        {
            if (_mapData == null || _gridView == null)
            {
                Debug.LogError("[Tactics] GameBootstrap needs a MapData and a GridView assigned.", this);
                return;
            }

            GridBuildResult result = _mapData.BuildGrid();
            if (!result.Success)
            {
                Debug.LogError($"[Tactics] {_mapData.name} failed to build:\n{string.Join("\n", result.Errors)}", _mapData);
                return;
            }

            _gridView.Build(result.Map, _mapData);
        }
    }
}
