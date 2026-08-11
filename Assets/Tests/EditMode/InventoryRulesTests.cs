using System.Collections.Generic;
using Assets.Resources.Scripts.Economy.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class InventoryRulesTests
    {
        [Test]
        public void Stackables_Merge_SameDefAndQuality()
        {
            var items = new List<InventoryStack>
            {
                new InventoryStack { itemDefId = "mat_scrap", quality = 2, quantity = 5 }
            };
            var incoming = new InventoryStack { itemDefId = "mat_scrap", quality = 2, quantity = 3 };
            Assert.IsTrue(InventoryRules.TryMerge(items, incoming, 60));
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(8, items[0].quantity);
        }

        [Test]
        public void Quality_Isolation_DoesNotMerge()
        {
            var items = new List<InventoryStack>
            {
                new InventoryStack { itemDefId = "mat_scrap", quality = 2, quantity = 5 }
            };
            var incoming = new InventoryStack { itemDefId = "mat_scrap", quality = 3, quantity = 2 };
            Assert.IsTrue(InventoryRules.TryMerge(items, incoming, 60));
            Assert.AreEqual(2, items.Count);
        }

        [Test]
        public void Equipment_NeverMerges()
        {
            var items = new List<InventoryStack>
            {
                new InventoryStack
                {
                    itemDefId = "eq_pulse_rifle",
                    quality = 2,
                    quantity = 1,
                    itemInstanceId = "a"
                }
            };
            var incoming = new InventoryStack
            {
                itemDefId = "eq_pulse_rifle",
                quality = 2,
                quantity = 1,
                itemInstanceId = "b"
            };
            Assert.IsTrue(InventoryRules.TryMerge(items, incoming, 60));
            Assert.AreEqual(2, items.Count);
            Assert.IsFalse(InventoryRules.CanStack(items[0], items[1]));
        }
    }
}
