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
        [Tooltip("Map plus starting units. The map comes from the deployment.")]
        [SerializeField] private DeploymentData _deployment;
        [SerializeField] private GridView _gridView;
        [SerializeField] private UnitsView _unitsView;
        [SerializeField] private SelectionController _selectionController;

        // Start, not Awake: every view has finished its own Awake setup by now.
        private void Start()
        {
            if (_deployment == null || _gridView == null || _unitsView == null || _selectionController == null)
            {
                Debug.LogError("[Tactics] GameBootstrap needs a DeploymentData, GridView, UnitsView and SelectionController assigned.", this);
                return;
            }

            BattleBuildResult result = _deployment.BuildBattle();
            if (!result.Success)
            {
                Debug.LogError($"[Tactics] {_deployment.name} failed to build:\n{string.Join("\n", result.Errors)}", _deployment);
                return;
            }

            _gridView.Build(result.Battle.Map, _deployment.Map);
            _unitsView.Build(result.Battle, _deployment);
            _selectionController.Initialize(result.Battle);
        }
    }
}
