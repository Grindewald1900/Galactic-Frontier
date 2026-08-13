using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Characters.Templates;

namespace Assets.Resources.Scripts.Characters.Roster
{
    public sealed class Vex : BurnStrikeTemplate
    {
        public Vex() : base(CharacterName.Vex, Archetype.Mechanician, 10) { }
    }

    public sealed class Nyx : BurnStrikeTemplate
    {
        public Nyx() : base(CharacterName.Nyx, Archetype.Assassin, 10) { }
    }

    public sealed class Rynn : BurnStrikeTemplate
    {
        public Rynn() : base(CharacterName.Rynn, Archetype.Potioneer, 8) { }
    }

    public sealed class Kael : StunFrontTemplate
    {
        public Kael() : base(CharacterName.Kael, Archetype.Warrior, 10) { }
    }

    public sealed class Ibalon : StunFrontTemplate
    {
        public Ibalon() : base(CharacterName.Ibalon, Archetype.Warrior, 10) { }
    }

    public sealed class Groth : StunFrontTemplate
    {
        public Groth() : base(CharacterName.Groth, Archetype.Monster, 10) { }
    }

    public sealed class Lyra : FreezeBlastTemplate
    {
        public Lyra() : base(CharacterName.Lyra, Archetype.Magician, 10) { }
    }

    public sealed class Orin : FreezeBlastTemplate
    {
        public Orin() : base(CharacterName.Orin, Archetype.Magician, 8) { }
    }
}
