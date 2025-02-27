using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CharacterSkillController
{
    public static List<Character> characters;

    public static void InitSkillSet()
    {
        characters = new List<Character>
        {
            new Asra(),
            new Magki(),
            new Sernia(),
        };
    }

    public static Character GetCharacter(CharacterName characterName)
    {
        return characters.FirstOrDefault(s => s.characterName == characterName);
    }
}

public enum CharacterName
{
    Asra,
    Magki,
    Sernia,
    Default
}