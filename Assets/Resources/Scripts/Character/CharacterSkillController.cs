using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        Debug.Log("skillSet: " + character.ToString());
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

public enum CharacterTier
{
    TierF, // Grey
    TierE, // Green
    TierD, // Blue
    TierC, // Purple
    TierB, // Yellow
    TierA, // Orange
    TierS, // Red
    TierSS, // Rainbow
}