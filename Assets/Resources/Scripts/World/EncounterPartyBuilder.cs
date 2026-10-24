using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    /// <summary>Builds enemy CardEntity lists from configured encounter profiles. Stats are not rolled.</summary>
    public static class EncounterPartyBuilder
    {
        public static List<CardEntity> Build(EncounterConfig encounter)
        {
            var result = new List<CardEntity>();
            if (encounter?.enemies == null)
                return result;

            for (var i = 0; i < encounter.enemies.Length; i++)
            {
                var slot = encounter.enemies[i];
                if (EnemyCatalog.IsEmptySlot(slot))
                {
                    result.Add(null);
                    continue;
                }

                var resolved = EnemyCatalog.Resolve(encounter, slot, i);
                if (resolved == null)
                {
                    Debug.LogWarning($"[ENCOUNTER] {encounter.encounterId} slot {i} did not resolve.");
                    result.Add(null);
                    continue;
                }

                var skill = ParseSkill(resolved.SkillKey);
                var entity = new CardEntity()
                    .SetCardName(resolved.DisplayNameEn)
                    .SetCharacterName(skill)
                    .SetCharacterTier(encounter.isBoss ? CharacterTier.TierA : CharacterTier.TierC)
                    .SetLevel(resolved.Level)
                    .SetExp(0f)
                    .SetAttack(resolved.Attack)
                    .SetDefense(resolved.Defense)
                    .SetHealth(resolved.Health)
                    .SetAccuracy(resolved.Accuracy)
                    .SetDodge(resolved.Dodge)
                    .SetCritical(resolved.Critical)
                    .SetCriticalDamage(resolved.CriticalDamage)
                    .SetEnergyGenerateRate((int)resolved.Energy)
                    .SetSpeed(resolved.Speed)
                    .SetId(resolved.ProfileId + "@" + encounter.encounterId + "#" + i);
                entity.portraitKey = EnemyCatalog.PlaceholderSprite;
                entity.enemyProfileId = resolved.ProfileId;
                entity.enemyPhaseIndex = 0;
                result.Add(entity);
                Debug.Log(
                    $"[ENCOUNTER] {encounter.encounterId} #{i} id={resolved.ProfileId} " +
                    $"name={resolved.DisplayNameEn} role={resolved.Role} lv={resolved.Level} " +
                    $"atk={resolved.Attack} def={resolved.Defense} hp={resolved.Health} " +
                    $"skill={resolved.SkillKey} power={resolved.Power} art={entity.portraitKey}");
            }

            return result;
        }

        private static CharacterName ParseSkill(string skillKey)
        {
            if (!string.IsNullOrEmpty(skillKey)
                && Enum.TryParse(skillKey, true, out CharacterName name))
                return name;
            return CharacterName.Asra;
        }
    }
}
