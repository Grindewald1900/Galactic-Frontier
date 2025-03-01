using UnityEngine;
using System.Collections.Generic;
using Assets.Resources.Scripts.Props;

namespace Assets.Resources.Scripts.Entity
{
    public class CardExpertiseEntity
    {
        public List<ExpertiseEntity> expertises;
        Dictionary<Status.AttributeType, float> attributes = new();

        public CardExpertiseEntity()
        {
            Init();
        }

        public void Init()
        {
            attributes.Add(Status.AttributeType.Health, 1f);
            attributes.Add(Status.AttributeType.Attack, 1f);
            attributes.Add(Status.AttributeType.Defense, 1f);
            attributes.Add(Status.AttributeType.Accuracy, 1f);
            attributes.Add(Status.AttributeType.Dodge, 1f);
            attributes.Add(Status.AttributeType.Critical, 1f);
            attributes.Add(Status.AttributeType.CriticalDamage, 1f);
            attributes.Add(Status.AttributeType.DamageReduction, 1f);
            attributes.Add(Status.AttributeType.EnergyGenerateRate, 1f);
            attributes.Add(Status.AttributeType.Speed, 1f);

            expertises = new List<ExpertiseEntity>();
            //TODO: Test Only - Remove
            ApplyExpertise(new ExpertiseEntity(Status.AttributeType.Health, 0.05f, ExpertiseTier.TIER_S));
            ApplyExpertise(new ExpertiseEntity(Status.AttributeType.Attack, 0.1f, ExpertiseTier.TIER_A));
            ApplyExpertise(new ExpertiseEntity(Status.AttributeType.Defense, 0.1f, ExpertiseTier.TIER_A));
            ApplyExpertise(new ExpertiseEntity(Status.AttributeType.Accuracy, 0.1f, ExpertiseTier.TIER_A));
            ApplyExpertise(new ExpertiseEntity(Status.AttributeType.Dodge, 0.05f, ExpertiseTier.TIER_A));
            ApplyExpertise(new ExpertiseEntity(Status.AttributeType.Critical, 0.2f, ExpertiseTier.TIER_A));
        }

        public void ApplyExpertise(ExpertiseEntity expertise)
        {
            attributes[expertise.attributeType] += expertise.value;
            expertises.Add(expertise);
        }

        public void RemoveExpertise(ExpertiseEntity expertise)
        {
            attributes[expertise.attributeType] -= expertise.value;
            expertises.Remove(expertise);
        }
    }
    public enum ExpertiseTier
    {
        TIER_S,
        TIER_A,
        TIER_B,
        TIER_C,
        TIER_D,
        TIER_E,
        TIER_F,
    }
}