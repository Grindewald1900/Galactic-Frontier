using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Asra : SkillSet
{
    private float attackMultiplier = 0.6f;
    private int debuffRound = 3;
    public Asra()
    {
        character = Character.Asra;
        archetype = Archetype.Mechanician;
    }

    public override void NormalAttack(Card player, List<Card> target)
    {
        int count = Mathf.Min(3, target.Count);
        List<Card> selectedTargets = target.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
        player.PlayAttackAnimation();
        // TODO:Test Only - Random Buff and Debuff
        Status.BuffType buffType = Status.GetRandomBuff();
        Status.DebuffType debuffType = Status.GetRandomDebuff();
        BuffEntity buff = new BuffEntity(buffType, 3, ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, buffType.ToString()));
        DebuffEntity debuff = new DebuffEntity(debuffType, 3, ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, debuffType.ToString()));

        foreach (var enermy in selectedTargets)
        {
            List<DamageEntity> damageEntities = new List<DamageEntity>();
            damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
            damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
            damageEntities.Add(new DamageEntity(0, DamageType.MISS, 1f));
            enermy.TakeDamage(damageEntities);
            enermy.buffManager.AddBuff(buff);
            enermy.debuffManager.AddDebuff(debuff);
        }
    }

    public override void SpecialAttack(Card player, List<Card> target)
    {
        int count = Mathf.Min(5, target.Count);
        List<Card> selectedTargets = target.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
        DebuffEntity debuff = new DebuffEntity(Status.DebuffType.Burning, debuffRound, ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, Status.DebuffType.Burning.ToString()));
        player.PlayAttackAnimation();
        foreach (var enermy in selectedTargets)
        {
            List<DamageEntity> damageEntities = new List<DamageEntity>();
            damageEntities.Add(BattleController.Instance.CalculateDamage(player, enermy, attackMultiplier));
            damageEntities[0].damageType = DamageType.SPECIAL_DAMAGE;
            enermy.TakeDamage(damageEntities);
            enermy.debuffManager.AddDebuff(debuff);
        }
    }

    public override void PassiveSkill(Card player, List<Card> target)
    {
    }

}