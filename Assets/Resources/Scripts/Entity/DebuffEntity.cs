using Assets.Resources.Scripts.Props;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.Entity
{
    [System.Serializable]
    public class DebuffEntity
    {
        public Status.DebuffType type;
        public Status.DamageType damageType;
        public Status.ControllType controllType;
        public Status.AttributeType attributeType;
        public int roundsRemaining;
        public float damage;
        public float attribute;
        public Sprite icon;
        public string name;

        public DebuffEntity() { }
        public DebuffEntity(
            Sprite icon, string name,
            Status.DebuffType type,
            Status.DamageType damageType = Status.DamageType.None,
            Status.ControllType controllType = Status.ControllType.None,
            Status.AttributeType attributeType = Status.AttributeType.None,
            int roundsRemaining = DefaultProperty.defaultDebuffRound, float damage = 0f, float attribute = 0f)
        {
            this.type = type;
            this.damageType = damageType;
            this.controllType = controllType;
            this.attributeType = attributeType;
            this.roundsRemaining = roundsRemaining;
            this.damage = damage;
            this.attribute = attribute;
            this.icon = icon;
            this.name = name;
        }

        public void Purify()
        {
            roundsRemaining = 0;
        }

        public DebuffEntity ChangeRounds(int rounds)
        {
            roundsRemaining += rounds;
            return this;
        }

        public DebuffEntity SetDebuffType(Status.DebuffType type)
        {
            this.type = type;
            return this;
        }

        public DebuffEntity SetDamageType(Status.DamageType damageType)
        {
            this.damageType = damageType;
            return this;
        }

        public DebuffEntity SetControllType(Status.ControllType controllType)
        {
            this.controllType = controllType;
            return this;
        }

        public DebuffEntity SetAttributeType(Status.AttributeType attributeType)
        {
            this.attributeType = attributeType;
            return this;
        }

        public DebuffEntity SetDamage(float damage)
        {
            this.damage = damage;
            return this;
        }

        public DebuffEntity SetAttribute(float attribute)
        {
            this.attribute = attribute;
            return this;
        }

        public DebuffEntity SetIcon(Sprite icon)
        {
            this.icon = icon;
            return this;
        }

        public DebuffEntity SetName(string name)
        {
            this.name = name;
            return this;
        }
    }
}