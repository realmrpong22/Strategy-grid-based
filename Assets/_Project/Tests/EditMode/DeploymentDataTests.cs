using NUnit.Framework;
using Tactics.Core;
using Tactics.Data;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class DeploymentDataTests
    {
        private TerrainTypeData _plain;
        private MapData _map;
        private UnitClassData _soldier;
        private DeploymentData _deployment;

        [SetUp]
        public void SetUp()
        {
            _plain = ScriptableObject.CreateInstance<TerrainTypeData>();
            _plain.SetValues("P", "Plain", 1, true, true, 0, 0, Color.green);
            _map = ScriptableObject.CreateInstance<MapData>();
            _map.SetLayout(3, 1, new[] { "P", "P", "P" }, new[] { 0, 0, 0 }, new[] { _plain });
            _soldier = ScriptableObject.CreateInstance<UnitClassData>();
            _soldier.SetValues("Soldier", "Soldier", 20, 5, 1, false, Color.grey);
            _deployment = ScriptableObject.CreateInstance<DeploymentData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_plain);
            Object.DestroyImmediate(_map);
            Object.DestroyImmediate(_soldier);
            Object.DestroyImmediate(_deployment);
        }

        [Test]
        public void BuildBattle_PlacesUnits_SharingOneDefinitionPerClass()
        {
            _deployment.SetValues(_map, new[]
            {
                new UnitPlacement(_soldier, Faction.Player, new Vector2Int(0, 0)),
                new UnitPlacement(_soldier, Faction.Enemy, new Vector2Int(2, 0)),
            });

            BattleBuildResult result = _deployment.BuildBattle();

            Assert.IsTrue(result.Success, string.Join("\n", result.Errors));
            Assert.AreEqual(2, result.Battle.Units.Count);
            Assert.IsTrue(result.Battle.TryGetUnitAt(new Vector2Int(2, 0), out Unit enemy));
            Assert.AreEqual(Faction.Enemy, enemy.Faction);
            Assert.AreSame(result.Battle.Units[0].Class, result.Battle.Units[1].Class);
        }

        [Test]
        public void BuildBattle_NoMap_Fails()
        {
            BattleBuildResult result = _deployment.BuildBattle();

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Battle);
        }

        [Test]
        public void BuildBattle_MissingClassReference_ReportsUnitNumber()
        {
            _deployment.SetValues(_map, new[] { new UnitPlacement(null, Faction.Player, new Vector2Int(0, 0)) });

            BattleBuildResult result = _deployment.BuildBattle();

            Assert.IsFalse(result.Success);
            StringAssert.Contains("unit 1", result.Errors[0]);
        }

        [Test]
        public void BuildBattle_InvalidPlacement_Fails()
        {
            _deployment.SetValues(_map, new[]
            {
                new UnitPlacement(_soldier, Faction.Player, new Vector2Int(1, 0)),
                new UnitPlacement(_soldier, Faction.Enemy, new Vector2Int(1, 0)),
            });

            BattleBuildResult result = _deployment.BuildBattle();

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Battle);
            StringAssert.Contains("unit 2", result.Errors[0]);
        }

        [Test]
        public void TryGetUnitClass_FindsById()
        {
            _deployment.SetValues(_map, new[] { new UnitPlacement(_soldier, Faction.Player, new Vector2Int(0, 0)) });

            Assert.IsTrue(_deployment.TryGetUnitClass("Soldier", out UnitClassData found));
            Assert.AreSame(_soldier, found);
            Assert.IsFalse(_deployment.TryGetUnitClass("Knight", out _));
        }
    }
}
