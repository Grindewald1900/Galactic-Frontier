using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Characters.Magician;
using Assets.Resources.Scripts.Characters.Mechanician;
using Assets.Resources.Scripts.Characters.Monster;
using Assets.Resources.Scripts.Characters;

namespace Assets.Resources.Scripts.CharacterPanel
{
    /// <summary>
    /// Runtime registry that maps persisted CharacterName values to combat strategy instances.
    /// BattleController queries this registry immediately before each card acts.
    /// </summary>
    public static class CharacterSkillController
    {
        public static List<Character> characters;

        /// <summary>Rebuilds the character strategy catalog for a new battle.</summary>
        public static void InitSkillSet()
        {
            characters = new List<Character>
        {
            new Asra(),
            new Magki(),
            new Sernia(),
        };
        }

        /// <summary>Returns the strategy matching a card's persisted character identity.</summary>
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
