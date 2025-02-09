using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Magki : SkillSet
{
    public Magki()
    {
        character = Character.Magki;
        archetype = Archetype.Monster;
    }

    public override void NormalAttack(Card player, List<Card> target)
    {
        player.PlayAttackAnimation();
    }

    public override void SpecialAttack(Card player, List<Card> target)
    {
        player.PlayAttackAnimation();
    }

    public override void PassiveSkill(Card player, List<Card> target)
    {
        player.PlayAttackAnimation();
    }
}