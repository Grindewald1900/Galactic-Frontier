using System.Collections.Generic;
using System.Collections;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using static Assets.Resources.Scripts.Cards.CardDataManager;

namespace Assets.Resources.Scripts.Characters
{
    /// <summary>
    /// Strategy contract for character-specific combat behavior and card-generation metadata.
    /// Concrete characters implement attacks as coroutines so animation, effects, and damage can
    /// remain in one ordered sequence controlled by BattleController.
    /// </summary>
    public abstract class Character
    {
        public CharacterName characterName;
        public Dictionary<CharacterTier, int> possibleTiers;
        public int weight = 10;
        public Archetype archetype;
        /// <summary>Executes the standard attack against the currently valid target set.</summary>
        public abstract IEnumerator NormalAttack(Card player, List<Card> target);

        /// <summary>Executes the energy-gated special attack.</summary>
        public abstract IEnumerator SpecialAttack(Card player, List<Card> target);

        /// <summary>Applies passive behavior for this character.</summary>
        public abstract void PassiveSkill(Card player, List<Card> target);

        /// <summary>Returns rarity weights used when CardDataManager generates this character.</summary>
        public abstract Dictionary<CharacterTier, int> GetPossibleTiers();
    }
}
