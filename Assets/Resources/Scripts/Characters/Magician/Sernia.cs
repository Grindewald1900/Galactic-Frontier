using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.CharacterPanel;
using static Assets.Resources.Scripts.Cards.CardDataManager;

namespace Assets.Resources.Scripts.Characters.Magician
{
    public class Sernia : Character
    {
        private readonly float attackMultiplier = 0.6f;
        private readonly int debuffRound = DefaultProperty.defaultDebuffRound;
        public Sernia()
        {
            characterName = CharacterName.Sernia;
            archetype = Archetype.Magician;
            possibleTiers = new Dictionary<CharacterTier, int>
            {
                { CharacterTier.TierSS, 10 },
                { CharacterTier.TierS, 100 },
                { CharacterTier.TierA, 500 },
                { CharacterTier.TierB, 1000 },
                { CharacterTier.TierC, 2000 },
                { CharacterTier.TierD, 5000 },
                { CharacterTier.TierE, 10000 }
            };
        }

        public override IEnumerator NormalAttack(Card player, List<Card> target)
        {
            List<Card> selectedTargets = TargetSelector.GetBackRowCards(target);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enermy in selectedTargets)
            {
                List<DamageEntity> damageEntities = new();
                damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
                damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
                damageEntities.Add(new DamageEntity(0, DamageType.MISS, 1f));
                enermy.TakeDamage(player, damageEntities);
                enermy.debuffManager.AddDebuff(GetDebuff(), enermy);
            }
            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override IEnumerator SpecialAttack(Card player, List<Card> target)
        {
            List<Card> selectedTargets = TargetSelector.GetAllCards(target);
            if (selectedTargets == null)
                yield break;

            player.PlayAttackAnimation();
            foreach (var enermy in selectedTargets)
            {
                List<DamageEntity> damageEntities = new()
                {
                    BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier)
                };
                damageEntities[0].damageType = DamageType.SPECIAL_DAMAGE;
                enermy.TakeDamage(player, damageEntities);
                enermy.debuffManager.AddDebuff(GetDebuff(), enermy);
            }
            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override void PassiveSkill(Card player, List<Card> target)
        {
            player.PlayAttackAnimation();
        }

        public override Dictionary<CharacterTier, int> GetPossibleTiers()
        {
            return possibleTiers;
        }

        private BuffEntity GetBuff()
        {
            return new BuffEntity().SetBuffType(Status.BuffType.AttributeUp)
            .SetAttributeType(Status.AttributeType.Attack)
            .SetAttribute(0.05f)
            .ChangeRounds(debuffRound)
            .SetIcon(ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "AttackUp"))
            .SetName("AttackUp");
        }

        private DebuffEntity GetDebuff()
        {
            return new DebuffEntity().SetDebuffType(Status.DebuffType.Controll)
            .SetControllType(Status.ControllType.Frozen)
            .ChangeRounds(debuffRound)
            .SetIcon(ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Frozen"))
            .SetName("Frozen");
        }
    }
}