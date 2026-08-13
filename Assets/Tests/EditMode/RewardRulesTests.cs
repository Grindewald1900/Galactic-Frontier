using System;
using System.Linq;
using Assets.Resources.Scripts.World.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class RewardRulesTests
    {
        [Test]
        public void Guaranteed_Entries_Always_Grant()
        {
            var table = RewardCatalog.Get("reward_outer_first");
            Assert.IsNotNull(table);
            var grants = RewardRules.Resolve(table, new Random(1));
            Assert.IsTrue(grants.Any(g => g.itemDefId == "credit" && g.quantity == 40));
            Assert.IsTrue(grants.Any(g => g.itemDefId == "mat_scrap" && g.quantity == 12));
        }

        [Test]
        public void Weighted_Chance_Zero_Never_Grants()
        {
            var table = new RewardTable
            {
                tableId = "t",
                weighted = { new LootEntry { itemDefId = "mat_scrap", qtyMin = 1, qtyMax = 1, chance = 0f } }
            };
            var grants = RewardRules.Resolve(table, new Random(42));
            Assert.AreEqual(0, grants.Count);
        }

        [Test]
        public void Region_Bindings_Point_To_Existing_Tables()
        {
            foreach (var region in RegionCatalog.All)
            {
                Assert.IsNotNull(RewardCatalog.Get(region.firstClearRewardId), region.regionId);
                Assert.IsNotNull(RewardCatalog.Get(region.repeatClearRewardId), region.regionId);
                Assert.IsNotNull(RewardCatalog.Get(region.farmRewardId), region.regionId);
            }
        }

        [Test]
        public void FirstClear_Flag_Then_Repeat()
        {
            var world = WorldRules.CreateNewPlayerWorld();
            var first = WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_main", 1);
            Assert.IsTrue(first.Success);
            Assert.IsTrue(first.WasFirstClear);

            var repeat = WorldRules.RegisterVictory(world, WorldConstants.OuterBeltId, "enc_outer_main", 2);
            Assert.IsTrue(repeat.Success);
            Assert.IsFalse(repeat.WasFirstClear);
        }
    }
}
