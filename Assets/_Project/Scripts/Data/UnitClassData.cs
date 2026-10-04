using Tactics.Core;
using UnityEngine;

namespace Tactics.Data
{
    /// <summary>
    /// One unit class, generated from unit_classes.csv by the importer. Manual inspector edits are
    /// overwritten on the next import; change the CSV instead.
    /// </summary>
    public sealed class UnitClassData : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private int _maxHp = 1;
        [SerializeField] private int _move;
        [SerializeField] private int _jump;
        [SerializeField] private bool _flies;
        [SerializeField] private Color _color = Color.grey;

        public string Id => _id;
        public string DisplayName => _displayName;
        /// <summary>Greybox tint for the unit's body.</summary>
        public Color Color => _color;

        /// <summary>Throws if the values are invalid; the importer calls this to validate.</summary>
        public UnitClassDefinition ToDefinition() => new UnitClassDefinition(_id, _maxHp, _move, _jump, _flies);

        internal void SetValues(
            string id, string displayName, int maxHp, int move, int jump, bool flies, Color color)
        {
            _id = id;
            _displayName = displayName;
            _maxHp = maxHp;
            _move = move;
            _jump = jump;
            _flies = flies;
            _color = color;
        }
    }
}
