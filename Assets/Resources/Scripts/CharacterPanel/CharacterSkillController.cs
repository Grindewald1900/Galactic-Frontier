using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Characters.Magician;
using Assets.Resources.Scripts.Characters.Mechanician;
using Assets.Resources.Scripts.Characters.Monster;
using Assets.Resources.Scripts.Characters.Roster;

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
            characters = BuildRoster();
        }

        /// <summary>Returns the strategy matching a card's persisted character identity.</summary>
        public static Character GetCharacter(CharacterName characterName)
        {
            if (characters == null || characters.Count == 0)
                InitSkillSet();
            return characters.FirstOrDefault(s => s.characterName == characterName);
        }

        public static List<Character> BuildRoster()
        {
            var list = new List<Character>
            {
                new Asra(),
                new Magki(),
                new Sernia(),
                new Ibalon(),
                new Vex(),
                new Kael(),
                new Lyra(),
                new Groth(),
                new Nyx(),
                new Orin(),
                new Rynn(),
            };
            list.AddRange(Batch2Characters.CreateAll());
            return list;
        }
    }

    public enum CharacterName
    {
        Default,
        Asra,
        Magki,
        Sernia,
        Ibalon,
        Vex,
        Kael,
        Lyra,
        Groth,
        Nyx,
        Orin,
        Rynn,
        Dax,
        Mira,
        Solen,
        Brann,
        Tess,
        Juno,
        Pike,
        Wren,
        Hale,
        Zara,
        Keth,
        Voss,
        Nira,
        Quill,
        Draven,
        Sable,
        Yara,
        Thorn,
        Cinder,
        Rook,
        Faye,
        Lumen,
        Ash,
        Korin,
        Vega,
    }
}
