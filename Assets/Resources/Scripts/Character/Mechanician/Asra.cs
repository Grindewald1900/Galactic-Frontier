using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Asra : SkillSet
{
    private float attackMultiplier = 0.6f;
    private int debuffRound = DefaultProperty.defaultDebuffRound;
    public Asra()
    {
        character = Character.Asra;
        archetype = Archetype.Mechanician;
    }

    public override IEnumerator NormalAttack(Card player, List<Card> target)
    {
        List<Card> selectedTargets = TargetSelector.GetFrontRowCards(target);
        if (selectedTargets == null)
            yield break;
        player.PlayAttackAnimation();
        foreach (var enermy in selectedTargets)
        {
            List<DamageEntity> damageEntities = new List<DamageEntity>();
            damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
            damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
            damageEntities.Add(new DamageEntity(0, DamageType.MISS, 1f));
            enermy.TakeDamage(damageEntities);
            enermy.debuffManager.AddDebuff(GetDebuff(player), enermy);
        }
        yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
    }

    public override IEnumerator SpecialAttack(Card player, List<Card> target)
    {
        List<Card> selectedTargets = TargetSelector.GetRandomCards(target, 5);
        if (selectedTargets == null)
            yield break;
        player.PlayAttackAnimation();
        foreach (var enermy in selectedTargets)
        {
            List<DamageEntity> damageEntities = new List<DamageEntity>();
            damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
            damageEntities[0].damageType = DamageType.SPECIAL_DAMAGE;
            enermy.TakeDamage(damageEntities);
            enermy.debuffManager.AddDebuff(GetDebuff(player), enermy);
        }
        yield return new WaitForSeconds(DefaultProperty.defaultAttackTime);
    }

    public override void PassiveSkill(Card player, List<Card> target)
    {
    }

    private BuffEntity GetBuff(Card player)
    {
        return new BuffEntity().SetBuffType(Status.BuffType.AttributeUp)
        .SetAttributeType(Status.AttributeType.Attack)
        .SetAttribute(0.05f)
        .ChangeRounds(debuffRound)
        .SetIcon(ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "AttackUp"))
        .SetName("AttackUp");
    }

    private DebuffEntity GetDebuff(Card player)
    {
        return new DebuffEntity().SetDebuffType(Status.DebuffType.Damage)
        .SetDamageType(Status.DamageType.Burning)
        .SetDamage(player.cardEntity.attack * 0.15f)
        .ChangeRounds(debuffRound)
        .SetIcon(ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Burning"))
        .SetName("Burning");
    }
}