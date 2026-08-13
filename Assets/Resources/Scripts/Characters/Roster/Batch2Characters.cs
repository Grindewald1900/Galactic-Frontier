using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Characters.Templates;

namespace Assets.Resources.Scripts.Characters.Roster
{
    /// <summary>P5.1b roster expansion toward the 30–50 MVP target.</summary>
    public static class Batch2Characters
    {
        public static IEnumerable<Character> CreateAll()
        {
            yield return new RosterBurn(CharacterName.Dax, Archetype.Mechanician);
            yield return new RosterStun(CharacterName.Mira, Archetype.Warrior);
            yield return new RosterFreeze(CharacterName.Solen, Archetype.Magician);
            yield return new RosterStun(CharacterName.Brann, Archetype.Monster);
            yield return new RosterBurn(CharacterName.Tess, Archetype.Potioneer, 8);
            yield return new RosterBurn(CharacterName.Juno, Archetype.Assassin);
            yield return new RosterStun(CharacterName.Pike, Archetype.Warrior);
            yield return new RosterFreeze(CharacterName.Wren, Archetype.Magician, 8);
            yield return new RosterBurn(CharacterName.Hale, Archetype.Mechanician);
            yield return new RosterBurn(CharacterName.Zara, Archetype.Assassin);
            yield return new RosterStun(CharacterName.Keth, Archetype.Monster);
            yield return new RosterFreeze(CharacterName.Voss, Archetype.Magician);
            yield return new RosterBurn(CharacterName.Nira, Archetype.Potioneer, 8);
            yield return new RosterStun(CharacterName.Quill, Archetype.Warrior);
            yield return new RosterBurn(CharacterName.Draven, Archetype.Mechanician);
            yield return new RosterBurn(CharacterName.Sable, Archetype.Assassin, 8);
            yield return new RosterFreeze(CharacterName.Yara, Archetype.Magician);
            yield return new RosterStun(CharacterName.Thorn, Archetype.Monster);
            yield return new RosterBurn(CharacterName.Cinder, Archetype.Potioneer);
            yield return new RosterStun(CharacterName.Rook, Archetype.Warrior, 8);
            yield return new RosterFreeze(CharacterName.Faye, Archetype.Magician);
            yield return new RosterBurn(CharacterName.Lumen, Archetype.Mechanician, 8);
            yield return new RosterBurn(CharacterName.Ash, Archetype.Assassin);
            yield return new RosterStun(CharacterName.Korin, Archetype.Monster, 8);
            yield return new RosterFreeze(CharacterName.Vega, Archetype.Magician);
        }
    }
}
