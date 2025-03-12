using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Characters.Magician;
using Assets.Resources.Scripts.Characters.Mechanician;
using Assets.Resources.Scripts.Characters.Monster;
using Assets.Resources.Scripts.Characters;

namespace Assets.Resources.Scripts.CharacterPanel
{
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
        Default,
        Asra,
        Magki,
        Sernia,
        Ibalon,
    }
}