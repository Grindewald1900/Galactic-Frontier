using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>
    /// Default sample-data factory for Editor / explicit Dev Data Mode.
    /// Does not write save files.
    /// </summary>
    public sealed class DefaultDevDataProvider : IDevDataProvider
    {
        private static readonly string[] SampleItemIcons =
            { "Copper", "Steel", "GoldBar", "SteelBar", "Water", "Wood" };

        public bool IsActive => DevDataSettings.Enabled;

        public IReadOnlyList<ItemEntity> CreateSampleInventory(bool isRemote)
        {
            if (!IsActive)
                return System.Array.Empty<ItemEntity>();

            var items = new List<ItemEntity>(20);
            for (var i = 0; i < 20; i++)
            {
                items.Add(new ItemEntity(
                    $"Item {i}",
                    $"Description {i}",
                    SampleItemIcons[Random.Range(0, SampleItemIcons.Length)],
                    10 * i,
                    ItemType.Material)
                {
                    quantity = Random.Range(1, 111),
                    isRemote = isRemote
                });
            }

            return items;
        }

        public IReadOnlyList<CardEntity> CreateSampleEnemyParty(int count)
        {
            if (!IsActive || count <= 0)
                return System.Array.Empty<CardEntity>();

            var cardDataMgr = CardDataManager.Instance;
            if (cardDataMgr == null)
            {
                Debug.LogWarning("[DEV-DATA] CardDataManager missing; cannot create sample enemies.");
                return System.Array.Empty<CardEntity>();
            }

            var party = new List<CardEntity>(count);
            for (var i = 0; i < count; i++)
            {
                var character = cardDataMgr.GetCharacter();
                if (character == null)
                    continue;
                var entity = cardDataMgr.GetCardEntity(character);
                if (entity != null)
                    party.Add(entity);
            }

            return party;
        }

        public void FillSampleGachaMaterials(
            List<ItemEntity> providers,
            List<ItemEntity> consumers,
            List<int> quantitiesPerDraw)
        {
            if (providers == null || consumers == null || quantitiesPerDraw == null)
                return;

            providers.Clear();
            consumers.Clear();
            quantitiesPerDraw.Clear();

            if (!IsActive)
                return;

            var randomItemCounts = Random.Range(3, 6);
            for (var i = 0; i < randomItemCounts; i++)
            {
                var randomCount = Random.Range(10, 20);
                var providerItem = new ItemEntity(
                    "Item " + i,
                    "Description " + i,
                    SampleItemIcons[Random.Range(0, SampleItemIcons.Length)],
                    10 * i,
                    ItemType.Material);
                providerItem.SetQuantity(Random.Range(100, 500));
                var consumerItem = DeepCopyUtil.DeepCopy<ItemEntity>(providerItem);
                consumerItem.SetQuantity(0);
                providers.Add(providerItem);
                consumers.Add(consumerItem);
                quantitiesPerDraw.Add(randomCount);
            }
        }

        public IReadOnlyList<CardEntity> CreateSampleGachaResults(int count)
        {
            if (!IsActive || count <= 0)
                return System.Array.Empty<CardEntity>();

            var cardDataMgr = CardDataManager.Instance;
            if (cardDataMgr == null)
            {
                Debug.LogWarning("[DEV-DATA] CardDataManager missing; cannot create sample gacha results.");
                return System.Array.Empty<CardEntity>();
            }

            var results = new List<CardEntity>(count);
            for (var i = 0; i < count; i++)
            {
                var character = cardDataMgr.GetCharacter();
                if (character == null)
                    continue;
                var entity = cardDataMgr.GetCardEntity(character);
                if (entity != null)
                    results.Add(entity);
            }

            return results;
        }

        public PlanetEntity CreateSamplePlanet(int index)
        {
            if (!IsActive)
                return null;

            var randomIndex = Random.Range(0, 6);
            return new PlanetEntity("Planet_1", "Level 1", "Planet", "Description 1")
            {
                planetName = "Planet " + index,
                planetDescription = "Description " + randomIndex,
                planetLevel = "Level " + randomIndex,
                backgroundSprite = "Planet_" + randomIndex
            };
        }

        public IReadOnlyList<EventEntity> CreateSampleEvents(int count)
        {
            if (!IsActive || count <= 0)
                return System.Array.Empty<EventEntity>();

            var events = new List<EventEntity>(count);
            for (var i = 0; i < count; i++)
            {
                events.Add(new EventEntity(
                        "Event_" + i,
                        "Event " + i + " Description",
                        new System.DateTime(2025, 1, 1).ToString("yyyy-MM-dd"),
                        new System.DateTime(2025, 2, 1).ToString("yyyy-MM-dd"))
                    .SetEventType(MyEventType.New)
                    .SetActivationStatus(true));
            }

            return events;
        }
    }
}
