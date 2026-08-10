using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    /// <summary>Builds enemy CardEntity lists from EncounterConfig using CardDataManager.</summary>
    public static class EncounterPartyBuilder
    {
        public static List<CardEntity> Build(EncounterConfig encounter)
        {
            var result = new List<CardEntity>();
            if (encounter?.enemies == null)
                return result;

            var mgr = CardDataManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[ENCOUNTER] CardDataManager missing.");
                return result;
            }

            foreach (var slot in encounter.enemies)
            {
                if (slot == null) continue;
                if (!Enum.TryParse(slot.characterKey, true, out CharacterName name))
                    name = CharacterName.Asra;

                Character character = CharacterSkillController.GetCharacter(name);
                if (character == null)
                {
                    // Fallback: ask CardDataManager for any character.
                    character = mgr.GetCharacter();
                }

                if (character == null) continue;
                var entity = mgr.GetCardEntity(character);
                if (entity == null) continue;
                entity.Level = Math.Max(1, slot.level);
                entity.id = Guid.NewGuid().ToString("N");
                entity.cardName = (encounter.isBoss ? "[Boss] " : "") + entity.cardName;
                result.Add(entity);
            }

            return result;
        }
    }
}
