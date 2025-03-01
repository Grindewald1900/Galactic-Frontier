using Assets.Resources.Scripts.Props;

namespace Assets.Resources.Scripts.Entity
{
    public class ExpertiseEntity
    {
        public Status.AttributeType attributeType;
        public float value;
        public ExpertiseTier expertiseTier;

        public ExpertiseEntity(Status.AttributeType attributeType, float value, ExpertiseTier expertiseTier)
        {
            this.attributeType = attributeType;
            this.value = value;
            this.expertiseTier = expertiseTier;
        }

    }
}