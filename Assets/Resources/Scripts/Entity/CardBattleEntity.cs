using UnityEngine;
using System.Collections.Generic;
using Assets.Resources.Scripts.Props;
using System;

namespace Assets.Resources.Scripts.Entity
{
    public class CardBattleEntity
    {
        public Dictionary<Status.AttributeType, float> battleAttributes = new();

        public CardBattleEntity()
        {
            Init();
        }

        public void Init()
        {
            foreach (Status.AttributeType attribute in Enum.GetValues(typeof(Status.AttributeType)))
            {
                battleAttributes[attribute] = 1f;
            }
        }
        public void ChangeAttribute(Status.AttributeType attributeType, float value)
        {
            battleAttributes[attributeType] += value;
        }
    }
}