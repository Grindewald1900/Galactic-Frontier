namespace Assets.Resources.Scripts.Entity
{
    [System.Serializable]
    public class SpellEntity
    {
        public string spellName;
        public string spellDescription;
        public bool isUnlocked;
        public bool isActivated;
        public int spellLevel;

        public SpellEntity()
        {
            spellName = "Spell-Demo";
            spellDescription = "This is a demo spell.";
            isUnlocked = true;
            isActivated = true;
            spellLevel = 1;
        }
        public SpellEntity(string spellName, string spellDescription, bool isUnlocked, bool isActivated, int spellLevel)
        {
            this.spellName = spellName;
            this.spellDescription = spellDescription;
            this.isUnlocked = isUnlocked;
            this.isActivated = isActivated;
            this.spellLevel = spellLevel;
        }
    }
}