using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public abstract class SkillSet
{
    public Character character;
    public Archetype archetype;
    public abstract IEnumerator NormalAttack(Card player, List<Card> target);

    public abstract IEnumerator SpecialAttack(Card player, List<Card> target);

    public abstract void PassiveSkill(Card player, List<Card> target);
}
