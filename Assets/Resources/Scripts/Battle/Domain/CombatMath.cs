using System;

namespace Assets.Resources.Scripts.Battle.Domain
{
    /// <summary>
    /// Pure combat formulas from systems/02-auto-battle.md §7. No Unity dependencies.
    /// </summary>
    public static class CombatMath
    {
        public const double LogReductionBase = 1.4;

        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        public static float HitRate(float attackerAccuracy, float defenderDodge) =>
            Clamp01(attackerAccuracy - defenderDodge);

        /// <summary>
        /// 减伤率 = clamp(log₁.₄(max(防御, 1)) × 0.01 + 固定减伤, 0, 1)
        /// </summary>
        public static float DamageReductionRate(float battleDefense, float fixedDamageReduction)
        {
            var defense = Math.Max(battleDefense, 1f);
            var logTerm = Math.Log(defense) / Math.Log(LogReductionBase);
            return Clamp01((float)(logTerm * 0.01) + fixedDamageReduction);
        }

        /// <summary>
        /// Resolves hit → crit → damage using the provided RNG stream.
        /// Roll order is fixed: hit check, then crit check (only if hit).
        /// </summary>
        public static AttackRollResult ResolveAttack(
            float attackerAccuracy,
            float defenderDodge,
            float criticalChance,
            float criticalDamageMultiplier,
            float battleAttack,
            float battleDefense,
            float fixedDamageReduction,
            float attackMultiplier,
            BattleRng rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var hitRate = HitRate(attackerAccuracy, defenderDodge);
            var hitRoll = rng.Value;
            if (hitRoll >= hitRate)
            {
                return new AttackRollResult(
                    hit: false,
                    isCritical: false,
                    criticalMultiplier: 1f,
                    damage: 0f,
                    hitRate: hitRate,
                    reductionRate: 0f,
                    hitRoll: hitRoll,
                    critRoll: 0f);
            }

            var critRoll = rng.Value;
            var isCrit = critRoll < criticalChance;
            var critMul = isCrit ? criticalDamageMultiplier : 1f;
            var reduction = DamageReductionRate(battleDefense, fixedDamageReduction);
            var damage = battleAttack * critMul * (1f - reduction) * attackMultiplier;

            return new AttackRollResult(
                hit: true,
                isCritical: isCrit,
                criticalMultiplier: critMul,
                damage: damage,
                hitRate: hitRate,
                reductionRate: reduction,
                hitRoll: hitRoll,
                critRoll: critRoll);
        }
    }

    public readonly struct AttackRollResult
    {
        public bool Hit { get; }
        public bool IsCritical { get; }
        public float CriticalMultiplier { get; }
        public float Damage { get; }
        public float HitRate { get; }
        public float ReductionRate { get; }
        public float HitRoll { get; }
        public float CritRoll { get; }

        public AttackRollResult(
            bool hit,
            bool isCritical,
            float criticalMultiplier,
            float damage,
            float hitRate,
            float reductionRate,
            float hitRoll,
            float critRoll)
        {
            Hit = hit;
            IsCritical = isCritical;
            CriticalMultiplier = criticalMultiplier;
            Damage = damage;
            HitRate = hitRate;
            ReductionRate = reductionRate;
            HitRoll = hitRoll;
            CritRoll = critRoll;
        }
    }
}
