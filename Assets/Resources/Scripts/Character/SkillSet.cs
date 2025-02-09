using UnityEngine;
using System.Collections.Generic;
public abstract class SkillSet
{
    public Character character;
    public Archetype archetype;
    public abstract void NormalAttack(Card player, List<Card> target);

    public abstract void SpecialAttack(Card player, List<Card> target);

    public abstract void PassiveSkill(Card player, List<Card> target);
}
