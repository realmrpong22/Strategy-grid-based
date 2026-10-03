using System;
using System.Collections.Generic;
using Tactics.Core;
using Tactics.Data;
using UnityEngine;

namespace Tactics.View
{
    /// <summary>
    /// Spawns one TileView per Core tile under this transform and converts between grid and world space.
    /// Move or rotate this GameObject to place the whole map; don't scale it.
    /// </summary>
    public sealed class GridView : MonoBehaviour
    {
        [SerializeField] private TileView _tilePrefab;
        [Tooltip("Lit material used as the template for each terrain's greybox colour.")]
        [SerializeField] private Material _tileBaseMaterial;
        [SerializeField, Min(0.01f)] private float _cellSize = 1f;
        [SerializeField, Min(0.01f)] private float _heightStep = 0.5f;
        [Tooltip("Fraction of the cell each column covers. Below 1 leaves gaps so individual tiles are readable.")]
        [SerializeField, Range(0.5f, 1f)] private float _tileFootprint = 0.95f;

        private readonly Dictionary<string, Material> _terrainMaterials = new Dictionary<string, Material>();
        private GridMetrics _metrics;
        private TileView[,] _tileViews;

        /// <summary>Raised after Build has spawned every tile.</summary>
        public event Action Built;

        public GridMap Map { get; private set; }
        public GridMetrics Metrics => _metrics;

        private void Awake()
        {
            _metrics = new GridMetrics(_cellSize, _heightStep);
        }

        public void Build(GridMap map, MapData mapData)
        {
            Clear();
            Map = map;
            _tileViews = new TileView[map.Width, map.Depth];

            foreach (Tile tile in map.AllTiles())
            {
                TileView view = Instantiate(_tilePrefab, transform);
                view.name = $"Tile ({tile.Position.x}, {tile.Position.y})";
                view.transform.localPosition = _metrics.GridToLocal(tile.Position, tile.Height);
                view.Initialize(tile, _metrics, _tileFootprint, GetTerrainMaterial(tile.Terrain.Id, mapData));
                _tileViews[tile.Position.x, tile.Position.y] = view;
            }

            Built?.Invoke();
        }

        /// <summary>World position of the map's centre at ground level (y = 0 in grid space).</summary>
        public Vector3 CenterWorld =>
            Map == null
                ? transform.position
                : transform.TransformPoint(new Vector3(
                    (Map.Width - 1) * _metrics.CellSize * 0.5f, 0f, (Map.Depth - 1) * _metrics.CellSize * 0.5f));

        /// <summary>Clamps x/z to the span of tile centres, in grid space so a rotated grid still works.</summary>
        public Vector3 ClampToGrid(Vector3 worldPosition)
        {
            if (Map == null)
                return worldPosition;

            Vector3 local = transform.InverseTransformPoint(worldPosition);
            local.x = Mathf.Clamp(local.x, 0f, (Map.Width - 1) * _metrics.CellSize);
            local.z = Mathf.Clamp(local.z, 0f, (Map.Depth - 1) * _metrics.CellSize);
            return transform.TransformPoint(local);
        }

        public bool TryGetTileView(Vector2Int position, out TileView view)
        {
            if (Map == null || !Map.InBounds(position))
            {
                view = null;
                return false;
            }

            view = _tileViews[position.x, position.y];
            return true;
        }

        /// <summary>World position of the centre of a tile's top surface.</summary>
        public Vector3 GridToWorld(Vector2Int position, int height) =>
            transform.TransformPoint(_metrics.GridToLocal(position, height));

        /// <summary>Nearest grid coordinate; may be out of bounds.</summary>
        public Vector2Int WorldToGrid(Vector3 worldPosition) =>
            _metrics.LocalToGrid(transform.InverseTransformPoint(worldPosition));

        /// <summary>
        /// One shared material per terrain type. Shared (not per-tile) so tiles stay SRP Batcher friendly
        /// and we don't create 120 material copies.
        /// </summary>
        private Material GetTerrainMaterial(string terrainId, MapData mapData)
        {
            if (_terrainMaterials.TryGetValue(terrainId, out Material material))
                return material;

            material = new Material(_tileBaseMaterial) { name = $"Terrain_{terrainId} (runtime)" };
            if (mapData.TryGetTerrainType(terrainId, out TerrainTypeData terrainType))
                material.color = terrainType.Color;
            else
                Debug.LogWarning($"[Tactics] No TerrainTypeData for '{terrainId}' in {mapData.name}; using base colour.", mapData);

            _terrainMaterials.Add(terrainId, material);
            return material;
        }

        private void Clear()
        {
            if (_tileViews != null)
            {
                foreach (TileView view in _tileViews)
                {
                    if (view != null)
                        Destroy(view.gameObject);
                }
            }

            _tileViews = null;
            Map = null;
            DestroyMaterials();
        }

        // Materials created with `new Material` aren't owned by any GameObject, so they'd leak.
        private void OnDestroy() => DestroyMaterials();

        private void DestroyMaterials()
        {
            foreach (Material material in _terrainMaterials.Values)
                Destroy(material);
            _terrainMaterials.Clear();
        }
    }
}
