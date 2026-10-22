using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// Switches a configured boss to its next form when the current phase HP crosses the authored threshold.
    /// The same card stays on the board: name, tint, stats, and skill kit change, and the HP bar refills.
    /// </summary>
    public static class BossPhaseDirector
    {
        public static bool TryAdvance(Card card)
        {
            if (card?.cardEntity == null) return false;
            var entity = card.cardEntity;
            var profile = EnemyCatalog.Get(entity.enemyProfileId);
            var phases = profile?.phases;
            if (phases == null || phases.Length < 2) return false;

            var current = entity.enemyPhaseIndex;
            if (current < 0 || current >= phases.Length - 1) return false;

            var leaving = phases[current];
            if (!EnemyCatalog.ShouldLeavePhase(card.currentHealth, entity.Health, leaving.leaveAtHpRatio))
                return false;

            var nextIndex = current + 1;
            var next = phases[nextIndex];
            if (!System.Enum.TryParse(next.skillKey, true, out CharacterName skill))
                skill = CharacterName.Asra;

            entity.enemyPhaseIndex = nextIndex;
            entity.SetCardName(next.displayNameEn);
            entity.characterName = skill;
            entity.SetAttack(next.attack);
            entity.SetDefense(next.defense);
            entity.SetHealth(next.health);
            entity.SetSpeed(next.speed);
            card.currentHealth = next.health;
            card.SetName(next.displayNameEn);
            card.SetImage(entity);

            var tint = new Color(1f, 0.62f, 0.45f, 1f);
            if (card.characterImage != null)
                card.characterImage.color = tint;
            if (card.baseColorImage != null)
                card.baseColorImage.color = tint;
            if (card.healthBar != null)
                card.healthBar.fillAmount = 1f;

            Debug.Log(
                $"[BOSS PHASE] {profile.profileId} {current + 1}->{nextIndex + 1} " +
                $"{next.displayNameEn} / {next.displayNameZh} skill={next.skillKey} " +
                $"hp={next.health} atk={next.attack} def={next.defense} | {next.logZh}");

            if (BattleInfo.Instance != null && !BattleInfo.Instance.isBattleInfoActive)
                BattleInfo.Instance.PlayBattleInfoAnimation(next.displayNameEn);

            return true;
        }
    }
}
