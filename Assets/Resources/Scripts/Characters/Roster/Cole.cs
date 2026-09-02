using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Utils;
using UnityEngine;
using static Assets.Resources.Scripts.Cards.CardDataManager;
namespace Assets.Resources.Scripts.Characters.Roster
{
    /// <summary>Frontier pilot — back-row striker for chapter 1 recruit tutorial.</summary>
    public class Cole : Character
    {
        private readonly float attackMultiplier = 0.65f;

        public Cole()
        {
            characterName = CharacterName.Cole;
            archetype = Archetype.Assassin;
            possibleTiers = new Dictionary<CharacterTier, int>
            {
                { CharacterTier.TierSS, 8 },
                { CharacterTier.TierS, 80 },
                { CharacterTier.TierA, 400 },
                { CharacterTier.TierB, 900 },
                { CharacterTier.TierC, 1800 },
                { CharacterTier.TierD, 4500 },
                { CharacterTier.TierE, 9000 }
            };
        }

        public override IEnumerator NormalAttack(Card player, List<Card> target)
        {
            var selectedTargets = TargetSelector.GetBackRowCards(target);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enemy in selectedTargets)
            {
                var damage = BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier);
                enemy.TakeDamage(player, new List<DamageEntity> { damage });
            }

            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override IEnumerator SpecialAttack(Card player, List<Card> target)
        {
            var selectedTargets = TargetSelector.GetBackRowCards(target);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enemy in selectedTargets)
            {
                var damage = BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier * 1.4f);
                damage.damageType = DamageType.SPECIAL_DAMAGE;
                enemy.TakeDamage(player, new List<DamageEntity> { damage });
            }

            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override void PassiveSkill(Card player, List<Card> target) { }

        public override Dictionary<CharacterTier, int> GetPossibleTiers() => possibleTiers;
    }
}
