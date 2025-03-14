namespace Assets.Resources.Scripts.Entity
{
    [System.Serializable]
    public class BaseAttrEntity
    {
        public int level;
        public float health;
        public float attack;
        public float defense;
        public float accuracy;
        public float dodge;
        public float critical;
        public float criticalDamage;
        public float damageReduction;
        public float energyGenerateRate;
        public float speed;

        public BaseAttrEntity() { }
    }
}