using NUnit.Framework;
using Tactics.Core;
using Tactics.Data;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class UnitClassDataTests
    {
        [Test]
        public void ToDefinition_CarriesValues()
        {
            var data = ScriptableObject.CreateInstance<UnitClassData>();
            try
            {
                data.SetValues("Cavalier", "Cavalier", 22, 7, 1, false, Color.red);

                UnitClassDefinition definition = data.ToDefinition();

                Assert.AreEqual("Cavalier", definition.Id);
                Assert.AreEqual(22, definition.MaxHp);
                Assert.AreEqual(7, definition.Move);
                Assert.AreEqual(1, definition.Jump);
                Assert.IsFalse(definition.Flies);
                Assert.AreEqual(Color.red, data.Color);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }
    }
}
