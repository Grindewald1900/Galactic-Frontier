using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>
    /// One configured phase of a boss. Phase 0 is the form the fight starts in.
    /// <see cref="leaveAtHpRatio"/> 0 means "when this phase's HP is depleted, enter the next form".
    /// </summary>
    public sealed class EnemyPhase
    {
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string role = "";
        public string skillKey = "Asra";
        public float attack;
        public float defense;
        public float health;
        public float speed = 18f;
        public float leaveAtHpRatio;
        public string logEn = "";
        public string logZh = "";
    }

    public sealed class EnemyProfile
    {
        public string profileId = "";
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string role = "";
        public string skillKey = "Asra";
        public int level = 1;
        public float attack;
        public float defense;
        public float health;
        public float speed = 18f;
        public EnemyPhase[] phases;
    }

    /// <summary>Deterministic enemy resolved from a profile or the shared level curve.</summary>
    public sealed class ResolvedEnemy
    {
        public string ProfileId;
        public string DisplayNameEn;
        public string DisplayNameZh;
        public string Role;
        public string SkillKey;
        public int Level;
        public float Attack;
        public float Defense;
        public float Health;
        public float Speed;
        public float Accuracy;
        public float Dodge;
        public float Critical;
        public float CriticalDamage;
        public float Energy;
        public int Power;
        public bool HasPhases;
    }

    /// <summary>
    /// Authored encounter enemies plus a deterministic curve for the rest of the encounter table.
    /// Recommended power uses the same terms as <c>CardEntity.CalculatePower</c>
    /// (HP + 5*ATK + 5*DEF + accuracy + dodge + crit + crit damage + energy + speed).
    /// The old stage hints (80–160) were a different scale from a level-1 card (~242) and are not used.
    /// </summary>
    public static class EnemyCatalog
    {
        public const string PlaceholderSprite = "EnemyPlaceholder";
        public const float Accuracy = 0.9f;
        public const float Dodge = 0.05f;
        public const float Critical = 0.05f;
        public const float CriticalDamage = 1.2f;
        public const float Energy = 10f;

        private static Dictionary<string, EnemyProfile> profiles;

        public static EnemyProfile Get(string profileId)
        {
            Ensure();
            if (string.IsNullOrEmpty(profileId)) return null;
            return profiles.TryGetValue(profileId, out var profile) ? profile : null;
        }

        public static bool IsEmptySlot(EncounterEnemySlot slot)
        {
            if (slot == null) return true;
            return string.IsNullOrEmpty(slot.profileId) && string.IsNullOrEmpty(slot.characterKey);
        }

        public static bool ShouldLeavePhase(float currentHp, float maxHp, float leaveAtHpRatio)
        {
            if (maxHp <= 0f) return true;
            if (leaveAtHpRatio <= 0f)
                return currentHp <= 0.01f;
            return currentHp / maxHp <= leaveAtHpRatio;
        }

        public static int Score(float health, float attack, float defense, float speed)
        {
            var raw = health
                + attack * 5f
                + defense * 5f
                + Accuracy
                + Dodge
                + Critical
                + CriticalDamage
                + Energy
                + speed;
            return (int)Math.Round(raw);
        }

        public static int RecommendedPower(string encounterId)
        {
            var encounter = EncounterCatalog.Get(encounterId);
            if (encounter?.enemies == null) return 0;
            var sum = 0;
            for (var i = 0; i < encounter.enemies.Length; i++)
            {
                var slot = encounter.enemies[i];
                if (IsEmptySlot(slot)) continue;
                var profile = string.IsNullOrEmpty(slot.profileId) ? null : Get(slot.profileId);
                if (profile?.phases != null && profile.phases.Length > 1)
                {
                    foreach (var phase in profile.phases)
                    {
                        if (phase == null) continue;
                        sum += Score(phase.health, phase.attack, phase.defense, phase.speed);
                    }

                    continue;
                }

                var resolved = Resolve(encounter, slot, i);
                if (resolved != null)
                    sum += resolved.Power;
            }

            return sum;
        }

        public static ResolvedEnemy Resolve(EncounterConfig encounter, EncounterEnemySlot slot, int index)
        {
            if (IsEmptySlot(slot)) return null;
            Ensure();
            if (!string.IsNullOrEmpty(slot.profileId))
            {
                var profile = Get(slot.profileId);
                return profile == null ? null : FromProfile(profile);
            }

            return FromCurve(encounter, slot, index);
        }

        private static void Ensure()
        {
            if (profiles != null) return;
            profiles = new Dictionary<string, EnemyProfile>();
            Add(Unit("drone_sweeper", "Sweep Drone", "清扫无人机", "Striker", "Asra", 1, 8f, 6f, 72f, 16f));

            Add(Unit("outer_lane_raider", "Lane Raider", "航道劫掠者", "Guard", "Magki", 1, 9f, 9f, 96f, 16f));
            Add(Unit("outer_lane_scout", "Lane Scout", "航道侦察艇", "Raider", "Sernia", 1, 8f, 5f, 64f, 20f));

            Add(Unit("outer_wreck_bruiser", "Wreck Bruiser", "残骸打手", "Guard", "Magki", 2, 8f, 8f, 84f, 16f));
            Add(Unit("outer_salvage_guard", "Salvage Guard", "拾荒守卫", "Raider", "Sernia", 2, 7f, 6f, 64f, 18f));
            Add(Unit("outer_hold_piercer", "Hold Piercer", "货舱穿刺者", "Raider", "Sernia", 2, 8f, 5f, 68f, 20f));

            Add(Unit("outer_escort_guard", "Escort Guard", "护航卫兵", "Guard", "Magki", 2, 10f, 12f, 120f, 16f));
            Add(Unit("outer_fast_boat", "Fast Boat", "高速掠袭艇", "Raider", "Sernia", 3, 13f, 6f, 90f, 24f));

            Add(Unit("outer_fog_striker", "Fog Striker", "雾中打手", "Striker", "Asra", 3, 12f, 8f, 100f, 18f));
            Add(Unit("outer_fog_caster", "Fog Caster", "熵雾术士", "Raider", "Sernia", 3, 12f, 7f, 90f, 18f));

            Add(Unit("outer_elite_command", "Elite Command", "精英指挥舰", "Guard", "Magki", 4, 12f, 16f, 150f, 16f));
            Add(Unit("outer_blockade", "Blockade Striker", "封锁打击者", "Striker", "Asra", 3, 11f, 10f, 110f, 17f));
            Add(Unit("outer_elite_rear", "Rear Raider", "后排突袭者", "Raider", "Sernia", 4, 13f, 8f, 100f, 21f));

            Add(Boss("outer_flagship", "Shield Hull", "劫掠旗舰·护盾",
                Phase("Shield Hull", "劫掠旗舰·护盾", "Guard", "Magki", 12f, 14f, 180f, 16f, 0f,
                    "The raid flagship's shield hull collapses.",
                    "护盾破裂，劫掠旗舰露出核心，改为后排突袭。"),
                Phase("Core Breach", "劫掠旗舰·核心", "Raider", "Sernia", 16f, 8f, 140f, 22f, 0f,
                    "The flagship core strikes the rear line.",
                    "核心暴露，旗舰开始后排突袭。")));

            Add(Unit("outer_scrap_drone", "Scrap Drone", "残骸无人机", "Striker", "Asra", 1, 7f, 5f, 60f, 16f));

            Add(Boss("mining_leviathan", "Shell Plate", "矿核巨兽·甲壳",
                Phase("Shell Plate", "矿核巨兽·甲壳", "Guard", "Magki", 14f, 18f, 200f, 16f, 0f,
                    "The ore leviathan's shell cracks.",
                    "甲壳崩裂，矿核过载并开始灼烧。"),
                Phase("Overload Core", "矿核巨兽·过载", "Striker", "Asra", 18f, 9f, 150f, 20f, 0f,
                    "The exposed core overloads.",
                    "矿核过载，巨兽改为灼烧打击。")));
        }

        private static void Add(EnemyProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.profileId)) return;
            profiles[profile.profileId] = profile;
        }

        private static EnemyProfile Unit(
            string id, string nameEn, string nameZh, string role, string skill,
            int level, float attack, float defense, float health, float speed) =>
            new EnemyProfile
            {
                profileId = id,
                displayNameEn = nameEn,
                displayNameZh = nameZh,
                role = role,
                skillKey = skill,
                level = level,
                attack = attack,
                defense = defense,
                health = health,
                speed = speed
            };

        private static EnemyProfile Boss(string id, string nameEn, string nameZh, params EnemyPhase[] phases)
        {
            var first = phases != null && phases.Length > 0 ? phases[0] : null;
            return new EnemyProfile
            {
                profileId = id,
                displayNameEn = nameEn,
                displayNameZh = nameZh,
                role = first?.role ?? "Boss",
                skillKey = first?.skillKey ?? "Magki",
                level = 4,
                attack = first?.attack ?? 10f,
                defense = first?.defense ?? 10f,
                health = first?.health ?? 100f,
                speed = first?.speed ?? 16f,
                phases = phases ?? Array.Empty<EnemyPhase>()
            };
        }

        private static EnemyPhase Phase(
            string nameEn, string nameZh, string role, string skill,
            float attack, float defense, float health, float speed, float leaveAtHpRatio,
            string logEn, string logZh) =>
            new EnemyPhase
            {
                displayNameEn = nameEn,
                displayNameZh = nameZh,
                role = role,
                skillKey = skill,
                attack = attack,
                defense = defense,
                health = health,
                speed = speed,
                leaveAtHpRatio = leaveAtHpRatio,
                logEn = logEn,
                logZh = logZh
            };

        private static ResolvedEnemy FromProfile(EnemyProfile profile)
        {
            var phase = profile.phases != null && profile.phases.Length > 0 ? profile.phases[0] : null;
            var attack = phase?.attack ?? profile.attack;
            var defense = phase?.defense ?? profile.defense;
            var health = phase?.health ?? profile.health;
            var speed = phase?.speed ?? profile.speed;
            return new ResolvedEnemy
            {
                ProfileId = profile.profileId,
                DisplayNameEn = phase?.displayNameEn ?? profile.displayNameEn,
                DisplayNameZh = phase?.displayNameZh ?? profile.displayNameZh,
                Role = phase?.role ?? profile.role,
                SkillKey = phase?.skillKey ?? profile.skillKey,
                Level = Math.Max(1, profile.level),
                Attack = attack,
                Defense = defense,
                Health = health,
                Speed = speed,
                Accuracy = Accuracy,
                Dodge = Dodge,
                Critical = Critical,
                CriticalDamage = CriticalDamage,
                Energy = Energy,
                Power = Score(health, attack, defense, speed),
                HasPhases = profile.phases != null && profile.phases.Length > 1
            };
        }

        /// <summary>
        /// Later nodes keep the encounter table but no longer roll tier or attack.
        /// Level comes from the slot. Role comes from the skill key.
        /// </summary>
        private static ResolvedEnemy FromCurve(EncounterConfig encounter, EncounterEnemySlot slot, int index)
        {
            var level = Math.Max(1, slot.level);
            var skill = string.IsNullOrEmpty(slot.characterKey) ? "Asra" : slot.characterKey;
            var role = skill switch
            {
                "Magki" => "Guard",
                "Sernia" => "Raider",
                "Cole" => "Hunter",
                _ => "Striker"
            };
            var health = 70f + level * 20f;
            var attack = 7f + level * 1.5f;
            var defense = 5f + level * 1.5f;
            switch (role)
            {
                case "Guard":
                    health *= 1.15f;
                    defense += 3f;
                    attack -= 1f;
                    break;
                case "Raider":
                case "Hunter":
                    health *= 0.9f;
                    attack += 2f;
                    defense -= 1f;
                    break;
            }

            if (encounter != null && encounter.isBoss)
            {
                health *= 1.45f;
                attack += 2f;
                defense += 3f;
            }

            var speed = role == "Raider" || role == "Hunter" ? 22f : 16f;
            var nameEn = encounter == null || string.IsNullOrEmpty(encounter.displayName)
                ? role
                : encounter.displayName;
            var nameZh = encounter == null || string.IsNullOrEmpty(encounter.displayNameZh)
                ? role
                : encounter.displayNameZh;
            var suffix = role switch
            {
                "Guard" => "卫兵",
                "Raider" => "突袭者",
                "Hunter" => "猎手",
                _ => "打击者"
            };
            return new ResolvedEnemy
            {
                ProfileId = (encounter?.encounterId ?? "enc") + "#" + index,
                DisplayNameEn = nameEn + " " + role + " " + (index + 1),
                DisplayNameZh = nameZh + suffix + (index + 1),
                Role = role,
                SkillKey = skill,
                Level = level,
                Attack = attack,
                Defense = defense,
                Health = health,
                Speed = speed,
                Accuracy = Accuracy,
                Dodge = Dodge,
                Critical = Critical,
                CriticalDamage = CriticalDamage,
                Energy = Energy,
                Power = Score(health, attack, defense, speed),
                HasPhases = false
            };
        }
    }
}
