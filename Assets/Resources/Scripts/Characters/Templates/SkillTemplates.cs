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

namespace Assets.Resources.Scripts.Characters.Templates
{
    /// <summary>Shared skill template: front-row burn strikes (Asra pattern).</summary>
    public abstract class BurnStrikeTemplate : Character
    {
        private readonly float attackMultiplier = 0.6f;
        private readonly int debuffRound = DefaultProperty.defaultDebuffRound;

        protected BurnStrikeTemplate(CharacterName name, Archetype arch, int spawnWeight = 10)
        {
            characterName = name;
            archetype = arch;
            weight = spawnWeight;
            possibleTiers = DefaultTiers();
        }

        public override IEnumerator NormalAttack(Card player, List<Card> target)
        {
            List<Card> selectedTargets = TargetSelector.GetFrontRowCards(target);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enemy in selectedTargets)
            {
                List<DamageEntity> damageEntities = new()
                {
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier),
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier),
                    new DamageEntity(0, DamageType.MISS, 1f)
                };
                enemy.TakeDamage(player, damageEntities);
                enemy.debuffManager.AddDebuff(GetDebuff(player), enemy);
            }
            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override IEnumerator SpecialAttack(Card player, List<Card> target)
        {
            List<Card> selectedTargets = TargetSelector.GetRandomCards(target, 5);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enemy in selectedTargets)
            {
                var damageEntities = new List<DamageEntity>
                {
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier)
                };
                damageEntities[0].damageType = DamageType.SPECIAL_DAMAGE;
                enemy.TakeDamage(player, damageEntities);
                enemy.debuffManager.AddDebuff(GetDebuff(player), enemy);
            }
            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override void PassiveSkill(Card player, List<Card> target) { }

        public override Dictionary<CharacterTier, int> GetPossibleTiers() => possibleTiers;

        private DebuffEntity GetDebuff(Card player) =>
            new DebuffEntity().SetDebuffType(Status.DebuffType.Damage)
                .SetDamageType(Status.DamageType.Burning)
                .SetDamage(player.cardEntity.Attack * 0.15f)
                .ChangeRounds(debuffRound)
                .SetIcon(ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Burning"))
                .SetName("Burning");

        private static Dictionary<CharacterTier, int> DefaultTiers() => new()
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

    /// <summary>Shared skill template: front-row stun bursts (Magki pattern).</summary>
    public abstract class StunFrontTemplate : Character
    {
        private readonly float attackMultiplier = 0.6f;
        private readonly int debuffRound = DefaultProperty.defaultControllRound;

        protected StunFrontTemplate(CharacterName name, Archetype arch, int spawnWeight = 10)
        {
            characterName = name;
            archetype = arch;
            weight = spawnWeight;
            possibleTiers = DefaultTiers();
        }

        public override IEnumerator NormalAttack(Card player, List<Card> target)
        {
            List<Card> selectedTargets = TargetSelector.GetFrontRowCards(target);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enemy in selectedTargets)
            {
                List<DamageEntity> damageEntities = new()
                {
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier),
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier),
                    new DamageEntity(0, DamageType.MISS, 1f)
                };
                enemy.TakeDamage(player, damageEntities);
            }
            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override IEnumerator SpecialAttack(Card player, List<Card> target)
        {
            List<Card> selectedTargets = TargetSelector.GetFrontRowCards(target);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enemy in selectedTargets)
            {
                List<DamageEntity> damageEntities = new()
                {
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier),
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier),
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier)
                };
                damageEntities[0].damageType = DamageType.SPECIAL_DAMAGE;
                enemy.TakeDamage(player, damageEntities);
                enemy.debuffManager.AddDebuff(GetDebuff(), enemy);
            }
            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override void PassiveSkill(Card player, List<Card> target) => player.PlayAttackAnimation();

        public override Dictionary<CharacterTier, int> GetPossibleTiers() => possibleTiers;

        private DebuffEntity GetDebuff() =>
            new DebuffEntity().SetDebuffType(Status.DebuffType.Controll)
                .SetControllType(Status.ControllType.Stunned)
                .ChangeRounds(debuffRound)
                .SetIcon(ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Stunned"))
                .SetName("Stunned");

        private static Dictionary<CharacterTier, int> DefaultTiers() => new()
        {
            { CharacterTier.TierSS, 10 },
            { CharacterTier.TierS, 100 },
            { CharacterTier.TierA, 500 },
            { CharacterTier.TierB, 1000 },
            { CharacterTier.TierC, 2000 },
            { CharacterTier.TierD, 5000 }
        };
    }

    /// <summary>Shared skill template: back-row freeze (Sernia pattern).</summary>
    public abstract class FreezeBlastTemplate : Character
    {
        private readonly float attackMultiplier = 0.6f;
        private readonly int debuffRound = DefaultProperty.defaultDebuffRound;

        protected FreezeBlastTemplate(CharacterName name, Archetype arch, int spawnWeight = 10)
        {
            characterName = name;
            archetype = arch;
            weight = spawnWeight;
            possibleTiers = DefaultTiers();
        }

        public override IEnumerator NormalAttack(Card player, List<Card> target)
        {
            List<Card> selectedTargets = TargetSelector.GetBackRowCards(target);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enemy in selectedTargets)
            {
                var damageEntities = new List<DamageEntity>
                {
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier),
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier),
                    new DamageEntity(0, DamageType.MISS, 1f)
                };
                enemy.TakeDamage(player, damageEntities);
                enemy.debuffManager.AddDebuff(GetDebuff(), enemy);
            }
            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override IEnumerator SpecialAttack(Card player, List<Card> target)
        {
            List<Card> selectedTargets = TargetSelector.GetAllCards(target);
            if (selectedTargets == null)
                yield break;
            player.PlayAttackAnimation();
            foreach (var enemy in selectedTargets)
            {
                var damageEntities = new List<DamageEntity>
                {
                    BattleController.Instance.CalculateDamage(player, enemy, attackMultiplier)
                };
                damageEntities[0].damageType = DamageType.SPECIAL_DAMAGE;
                enemy.TakeDamage(player, damageEntities);
                enemy.debuffManager.AddDebuff(GetDebuff(), enemy);
            }
            yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
        }

        public override void PassiveSkill(Card player, List<Card> target) => player.PlayAttackAnimation();

        public override Dictionary<CharacterTier, int> GetPossibleTiers() => possibleTiers;

        private DebuffEntity GetDebuff() =>
            new DebuffEntity().SetDebuffType(Status.DebuffType.Controll)
                .SetControllType(Status.ControllType.Frozen)
                .ChangeRounds(debuffRound)
                .SetIcon(ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Frozen"))
                .SetName("Frozen");

        private static Dictionary<CharacterTier, int> DefaultTiers() => new()
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

    public sealed class RosterBurn : BurnStrikeTemplate
    {
        public RosterBurn(CharacterName name, Archetype arch, int spawnWeight = 10)
            : base(name, arch, spawnWeight) { }
    }

    public sealed class RosterStun : StunFrontTemplate
    {
        public RosterStun(CharacterName name, Archetype arch, int spawnWeight = 10)
            : base(name, arch, spawnWeight) { }
    }

    public sealed class RosterFreeze : FreezeBlastTemplate
    {
        public RosterFreeze(CharacterName name, Archetype arch, int spawnWeight = 10)
            : base(name, arch, spawnWeight) { }
    }
}
