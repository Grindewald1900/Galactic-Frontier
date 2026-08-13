using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.World.Domain;

namespace Assets.Resources.Scripts.Characters
{
    /// <summary>P5.2 character → faction label map (no gameplay effects).</summary>
    public static class CharacterFactionCatalog
    {
        public static string GetTag(CharacterName name) => name switch
        {
            CharacterName.Asra or CharacterName.Vex or CharacterName.Kael or CharacterName.Lyra
                or CharacterName.Ibalon or CharacterName.Rynn or CharacterName.Dax or CharacterName.Mira
                or CharacterName.Solen or CharacterName.Brann or CharacterName.Tess or CharacterName.Juno
                or CharacterName.Pike or CharacterName.Wren or CharacterName.Hale or CharacterName.Lumen
                or CharacterName.Ash => FactionTags.FrontierGuard,
            CharacterName.Magki or CharacterName.Sernia or CharacterName.Groth or CharacterName.Nyx
                or CharacterName.Orin or CharacterName.Zara or CharacterName.Keth or CharacterName.Voss
                or CharacterName.Nira or CharacterName.Quill or CharacterName.Draven or CharacterName.Sable
                or CharacterName.Yara or CharacterName.Thorn or CharacterName.Cinder or CharacterName.Rook
                or CharacterName.Faye or CharacterName.Korin or CharacterName.Vega => FactionTags.RiftSyndicate,
            _ => ""
        };

        public static string LabelEn(CharacterName name) => FactionTags.ShortEn(GetTag(name));
        public static string LabelZh(CharacterName name) => FactionTags.ShortZh(GetTag(name));
    }
}
