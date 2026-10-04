using System;
using NUnit.Framework;
using Tactics.Core;

namespace Tactics.Tests.EditMode
{
    public class TerrainDefinitionTests
    {
        [Test]
        public void Constructor_StoresValues()
        {
            var terrain = new TerrainDefinition("F", 2, true, false, 1, 20);

            Assert.AreEqual("F", terrain.Id);
            Assert.AreEqual(2, terrain.MoveCost);
            Assert.IsTrue(terrain.IsPassable);
            Assert.IsFalse(terrain.IsFlyable);
            Assert.AreEqual(1, terrain.DefenseBonus);
            Assert.AreEqual(20, terrain.AvoidBonus);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void Constructor_EmptyId_Throws(string id)
        {
            Assert.Throws<ArgumentException>(() => new TerrainDefinition(id, 1, true, true, 0, 0));
        }

        [Test]
        public void Constructor_PassableWithZeroCost_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainDefinition("P", 0, true, false, 0, 0));
        }

        [Test]
        public void Constructor_FlyableOnlyWithZeroCost_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TerrainDefinition("W", 0, false, true, 0, 0));
        }

        [Test]
        public void Constructor_UnenterableWithZeroCost_IsAllowed()
        {
            Assert.DoesNotThrow(() => new TerrainDefinition("X", 0, false, false, 0, 0));
        }
    }
}
