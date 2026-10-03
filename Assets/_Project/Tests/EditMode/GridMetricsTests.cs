using System;
using NUnit.Framework;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class GridMetricsTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void GridToLocal_UsesCellSizeAndHeightStep()
        {
            var metrics = new GridMetrics(2f, 0.5f);

            Vector3 local = metrics.GridToLocal(new Vector2Int(3, 4), 2);

            Assert.AreEqual(6f, local.x, Tolerance);
            Assert.AreEqual(1f, local.y, Tolerance);
            Assert.AreEqual(8f, local.z, Tolerance);
        }

        [Test]
        public void LocalToGrid_RoundTripsTileCentres()
        {
            var metrics = new GridMetrics(1f, 0.5f);

            for (int x = -2; x <= 5; x++)
            for (int z = -2; z <= 5; z++)
            {
                var position = new Vector2Int(x, z);
                Assert.AreEqual(position, metrics.LocalToGrid(metrics.GridToLocal(position, 3)));
            }
        }

        [TestCase(0.49f, 0)]
        [TestCase(0.5f, 1)]   // borders always go to the higher tile...
        [TestCase(1.5f, 2)]   // ...unlike Mathf.RoundToInt, which would give 2 here but 0 at 0.5
        [TestCase(-0.49f, 0)]
        [TestCase(-0.51f, -1)]
        public void LocalToGrid_BordersAreConsistent(float x, int expectedX)
        {
            var metrics = new GridMetrics(1f, 0.5f);
            Assert.AreEqual(expectedX, metrics.LocalToGrid(new Vector3(x, 0f, 0f)).x);
        }

        [Test]
        public void LocalToGrid_IgnoresHeight()
        {
            var metrics = new GridMetrics(1f, 0.5f);
            Assert.AreEqual(new Vector2Int(1, 2), metrics.LocalToGrid(new Vector3(1.1f, 99f, 1.9f)));
        }

        [TestCase(0f, 0.5f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 0.5f)]
        public void Constructor_NonPositive_Throws(float cellSize, float heightStep)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridMetrics(cellSize, heightStep));
        }
    }
}
