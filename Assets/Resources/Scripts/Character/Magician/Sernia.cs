using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class Sernia : SkillSet
{
    private float attackMultiplier = 0.6f;
    private int debuffRound = DefaultProperty.defaultDebuffRound;
    public Sernia()
    {
        character = Character.Sernia;
        archetype = Archetype.Magician;
    }

    public override void NormalAttack(Card player, List<Card> target)
    {
        List<Card> selectedTargets = TargetSelector.GetBackRowCards(target);
        if (selectedTargets == null) return;
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
    }

    public override void SpecialAttack(Card player, List<Card> target)
    {
        List<Card> selectedTargets = TargetSelector.GetAllCards(target);
        if (selectedTargets == null) return;
        player.PlayAttackAnimation();
        foreach (var enermy in selectedTargets)
        {
            List<DamageEntity> damageEntities = new List<DamageEntity>();
            damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
            damageEntities[0].damageType = DamageType.SPECIAL_DAMAGE;
            enermy.TakeDamage(damageEntities);
            enermy.debuffManager.AddDebuff(GetDebuff(player), enermy);
        }
    }

    public override void PassiveSkill(Card player, List<Card> target)
    {
        player.PlayAttackAnimation();
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
        return new DebuffEntity().SetDebuffType(Status.DebuffType.Controll)
        .SetControllType(Status.ControllType.Frozen)
        .ChangeRounds(debuffRound)
        .SetIcon(ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Frozen"))
        .SetName("Frozen");
    }
}