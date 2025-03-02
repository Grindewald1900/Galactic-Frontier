using System.Collections.Generic;
using System.Collections;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;

namespace Assets.Resources.Scripts.Characters
{
    public abstract class Character
    {
        public CharacterName characterName;
        public Dictionary<CharacterTier, int> possibleTiers;
        public int weight = 10;
        public Archetype archetype;
        public abstract IEnumerator NormalAttack(Card player, List<Card> target);

        public abstract IEnumerator SpecialAttack(Card player, List<Card> target);

        public abstract void PassiveSkill(Card player, List<Card> target);
        public abstract Dictionary<CharacterTier, int> GetPossibleTiers();
    }
}