using System.Linq;
using Assets.Resources.Scripts.World.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class UniverseGeneratorTests
    {
        [SetUp]
        public void SetUp() => UniverseMapCatalog.ResetCache();

        [Test]
        public void SameSeed_ProducesIdenticalGraph()
        {
            var a = UniverseGenerator.Generate(42);
            var b = UniverseGenerator.Generate(42);

            Assert.AreEqual(a.sectors.Count, b.sectors.Count);
            Assert.AreEqual(a.routes.Count, b.routes.Count);

            for (int i = 0; i < a.sectors.Count; i++)
            {
                Assert.AreEqual(a.sectors[i].sectorId, b.sectors[i].sectorId);
                Assert.AreEqual(a.sectors[i].x, b.sectors[i].x, 0.001f);
                Assert.AreEqual(a.sectors[i].y, b.sectors[i].y, 0.001f);
                Assert.AreEqual(a.sectors[i].kind, b.sectors[i].kind);
            }

            var routeKeysA = a.routes.Select(r => EdgeKey(r.fromId, r.toId)).OrderBy(x => x).ToList();
            var routeKeysB = b.routes.Select(r => EdgeKey(r.fromId, r.toId)).OrderBy(x => x).ToList();
            CollectionAssert.AreEqual(routeKeysA, routeKeysB);
        }

        [Test]
        public void DifferentSeeds_UsuallyDiffer()
        {
            var a = UniverseGenerator.Generate(1);
            var b = UniverseGenerator.Generate(2);
            bool sameLayout = a.sectors.Count == b.sectors.Count
                && a.sectors.Zip(b.sectors, (x, y) => x.sectorId == y.sectorId && x.x == y.x && x.y == y.y)
                    .All(match => match);
            Assert.IsFalse(sameLayout);
        }

        [Test]
        public void GeneratedUniverse_PassesValidation_ForSampleSeeds()
        {
            for (int seed = 1; seed <= 24; seed++)
            {
                var universe = UniverseGenerator.Generate(seed);
                Assert.IsTrue(universe.validation.isValid, IssueText(seed, universe));
                Assert.IsNotNull(Find(universe, WorldConstants.SectorId));
                Assert.IsNotNull(Find(universe, WorldConstants.SectorCoreId));
            }
        }

        [Test]
        public void FromPlayerId_IsDeterministicAndNonZero()
        {
            int once = UniverseSeedUtil.FromPlayerId("pilot_001");
            int twice = UniverseSeedUtil.FromPlayerId("pilot_001");
            Assert.AreEqual(once, twice);
            Assert.AreNotEqual(0, once);
        }

        [Test]
        public void UniverseMapCatalog_ConfiguresPinnedSectors()
        {
            UniverseMapCatalog.Configure(99);
            Assert.IsNotNull(UniverseMapCatalog.Get(WorldConstants.SectorId));
            Assert.IsNotNull(UniverseMapCatalog.Get(WorldConstants.SectorCoreId));
            Assert.GreaterOrEqual(UniverseMapCatalog.Sectors.Count, 15);
            Assert.Greater(UniverseMapCatalog.Edges.Count, 0);
        }

        private static GeneratedSector Find(GeneratedUniverse universe, string id)
        {
            foreach (var s in universe.sectors)
            {
                if (s != null && s.sectorId == id)
                    return s;
            }

            return null;
        }

        private static string EdgeKey(string a, string b) =>
            string.CompareOrdinal(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";

        private static string IssueText(int seed, GeneratedUniverse universe)
        {
            if (universe.validation?.issues == null || universe.validation.issues.Count == 0)
                return $"Seed {seed} failed validation.";
            return $"Seed {seed}: " + string.Join("; ",
                universe.validation.issues.Select(i => $"{i.code}: {i.message}"));
        }
    }
}
