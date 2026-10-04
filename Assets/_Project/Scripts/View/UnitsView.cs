using System.Collections.Generic;
using Tactics.Core;
using Tactics.Data;
using UnityEngine;

namespace Tactics.View
{
    /// <summary>
    /// Spawns one UnitView per Core unit under this transform and maps Unit → UnitView.
    /// Owns the runtime materials (one per class colour, one per faction), like GridView does for terrain.
    /// </summary>
    public sealed class UnitsView : MonoBehaviour
    {
        [SerializeField] private UnitView _unitPrefab;
        [SerializeField] private GridView _gridView;
        [Tooltip("Lit material used as the template for class and faction colours.")]
        [SerializeField] private Material _unitBaseMaterial;

        [Header("Faction colours (presentation only)")]
        [SerializeField] private Color _playerColor = new Color(0.2f, 0.45f, 1f);
        [SerializeField] private Color _enemyColor = new Color(0.9f, 0.2f, 0.2f);

        private readonly Dictionary<Unit, UnitView> _views = new Dictionary<Unit, UnitView>();
        private readonly Dictionary<string, Material> _classMaterials = new Dictionary<string, Material>();
        private readonly Dictionary<Faction, Material> _factionMaterials = new Dictionary<Faction, Material>();

        public void Build(BattleState battle, DeploymentData deployment)
        {
            Clear();

            foreach (Unit unit in battle.Units)
            {
                UnitView view = Instantiate(_unitPrefab, transform);
                view.name = $"{unit.Faction} {unit.Class.Id}";
                view.Initialize(unit, GetClassMaterial(unit.Class.Id, deployment), GetFactionMaterial(unit.Faction));
                view.PlaceAt(TileTopWorld(battle.Map, unit.Position));
                _views.Add(unit, view);
            }
        }

        public bool TryGetView(Unit unit, out UnitView view) => _views.TryGetValue(unit, out view);

        /// <summary>Where a unit standing on the tile has its feet.</summary>
        public Vector3 TileTopWorld(GridMap map, Vector2Int position) =>
            _gridView.GridToWorld(position, map.GetTile(position).Height);

        private Material GetClassMaterial(string classId, DeploymentData deployment)
        {
            if (_classMaterials.TryGetValue(classId, out Material material))
                return material;

            material = new Material(_unitBaseMaterial) { name = $"UnitClass_{classId} (runtime)" };
            if (deployment.TryGetUnitClass(classId, out UnitClassData classData))
                material.color = classData.Color;
            else
                Debug.LogWarning($"[Tactics] No UnitClassData for '{classId}' in {deployment.name}; using base colour.", deployment);

            _classMaterials.Add(classId, material);
            return material;
        }

        private Material GetFactionMaterial(Faction faction)
        {
            if (_factionMaterials.TryGetValue(faction, out Material material))
                return material;

            material = new Material(_unitBaseMaterial)
            {
                name = $"Faction_{faction} (runtime)",
                color = faction == Faction.Player ? _playerColor : _enemyColor,
            };
            _factionMaterials.Add(faction, material);
            return material;
        }

        private void Clear()
        {
            foreach (UnitView view in _views.Values)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }

            _views.Clear();
            DestroyMaterials();
        }

        // Materials created with `new Material` aren't owned by any GameObject, so they'd leak.
        private void OnDestroy() => DestroyMaterials();

        private void DestroyMaterials()
        {
            foreach (Material material in _classMaterials.Values)
                Destroy(material);
            foreach (Material material in _factionMaterials.Values)
                Destroy(material);
            _classMaterials.Clear();
            _factionMaterials.Clear();
        }
    }
}
