using UnityEngine;
using System.Collections.Generic;
using Assets.Resources.Scripts.Props;

namespace Assets.Resources.Scripts.Entity
{
    public class CardBattleEntity
    {
        readonly Dictionary<Status.AttributeType, float> attributes = new();

        public CardBattleEntity()
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
        }
        public void ChangeAttribute(Status.AttributeType attributeType, float value)
        {
            attributes[attributeType] += value;
        }
    }
}