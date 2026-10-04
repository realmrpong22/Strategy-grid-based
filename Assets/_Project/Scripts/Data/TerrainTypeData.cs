using Tactics.Core;
using UnityEngine;

namespace Tactics.Data
{
    /// <summary>
    /// One terrain type, generated from terrain.csv by the importer. Manual inspector edits are
    /// overwritten on the next import; change the CSV instead.
    /// Not named "TerrainData" to avoid clashing with UnityEngine.TerrainData.
    /// </summary>
    public sealed class TerrainTypeData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private int _moveCost = 1;
        [SerializeField] private bool _isPassable = true;
        [SerializeField] private bool _isFlyable = true;
        [SerializeField] private int _defenseBonus;
        [SerializeField] private int _avoidBonus;
        [SerializeField] private Color _color = Color.grey;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Color Color => _color;

        /// <summary>Throws if the values are invalid; the importer calls this to validate.</summary>
        public TerrainDefinition ToDefinition() =>
            new TerrainDefinition(_id, _moveCost, _isPassable, _isFlyable, _defenseBonus, _avoidBonus);

        internal void SetValues(
            string id, string displayName, int moveCost, bool isPassable, bool isFlyable,
            int defenseBonus, int avoidBonus, Color color)
        {
            _id = id;
            _displayName = displayName;
            _moveCost = moveCost;
            _isPassable = isPassable;
            _isFlyable = isFlyable;
            _defenseBonus = defenseBonus;
            _avoidBonus = avoidBonus;
            _color = color;
        }
    }
}
