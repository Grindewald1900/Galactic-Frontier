using System.Collections.Generic;
using System.Linq;
public static class CharacterSkillController
{
    public static List<SkillSet> skillSet;

    public static void InitSkillSet()
    {
        skillSet = new List<SkillSet>
        {
            new Asra(),
            new Magki(),
            new Sernia(),
        };
    }

    public static SkillSet GetSkillSet(Character character)
    {
        return skillSet.FirstOrDefault(s => s.character == character);
    }

}

public enum Character
{
    Asra,
    Magki,
    Sernia,
}

public enum Archetype
{
    Assassin,
    Magician,
    Mechanician,
    Monster,
    Potioneer,
    Warrior,
}

