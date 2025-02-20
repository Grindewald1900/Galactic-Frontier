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
        this.spellName = "Spell-Demo";
        this.spellDescription = "This is a demo spell.";
        this.isUnlocked = true;
        this.isActivated = true;
        this.spellLevel = 1;
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