using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Progression.Domain;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using UnityEngine;

namespace Assets.Resources.Scripts.Progression
{
    /// <summary>Unity façade for commander / combat / profession XP grants and energy-rank ascend.</summary>
    public static class ProgressionService
    {
        public sealed class SpecPrompt
        {
            public CardEntity Card;
            public ProfessionSkill Skill;
            public int Milestone;
            public ProfessionSpec Recommended;
        }

        public sealed class GrantLog
        {
            public readonly List<string> NotesEn = new List<string>();
            public readonly List<string> NotesZh = new List<string>();
            public readonly List<SpecPrompt> Specs = new List<SpecPrompt>();
            public int CommanderLevels;
            public int CommanderLevelAfter;
            public bool CommanderLeveled => CommanderLevels > 0;
        }

        public static GrantLog LastLog { get; private set; }

        public static GrantLog BeginGrant()
        {
            CardEntity.BeginBatchPersist();
            LastLog = new GrantLog();
            return LastLog;
        }

        public static void EndGrant(bool presentUi)
        {
            CardEntity.EndBatchPersist(true);
            SaveCommander();
            if (LastLog != null && LastLog.CommanderLeveled)
            {
                RefreshDeckScaleUnlocks();
                FeatureUnlockService.Evaluate();
            }

            ProgressChanged?.Invoke();

            if (presentUi || (LastLog != null && LastLog.Specs.Count > 0))
                PresentLastLog();
        }

        /// <summary>Fired after every grant batch so shell/formation XP bars can refresh live.</summary>
        public static event System.Action ProgressChanged;

        public static void GrantCombatToParty(IList<CardEntity> members, IList<bool> knockedOut, float baseXp)
        {
            if (members == null || baseXp <= 0f) return;
            for (var i = 0; i < members.Count; i++)
            {
                var card = members[i];
                if (card == null) continue;
                var ko = knockedOut != null && i < knockedOut.Count && knockedOut[i];
                var share = ProgressionRules.JobShare(i, combat: true, knockedOut: ko);
                ApplyCombat(card, baseXp * share);
            }
        }

        public static void GrantProfessionToParty(
            IList<CardEntity> members,
            ProfessionSkill skill,
            int requiredLevel,
            float seconds)
        {
            if (members == null || skill == ProfessionSkill.None || seconds <= 0f) return;
            for (var i = 0; i < members.Count; i++)
            {
                var card = members[i];
                if (card == null) continue;
                var share = ProgressionRules.JobShare(i, combat: false, knockedOut: false);
                var match = ProgressionRules.MatchMultiplier(card.GetProfessionLevel(skill), requiredLevel);
                var aptitude = ProgressionCatalog.Aptitude(card.characterName.ToString(), skill);
                var amount = ProgressionRules.ScaledXp(
                    ProgressionCatalog.ProfessionXpPerSecond, seconds, match, share, aptitude);
                ApplyProfession(card, skill, amount);
            }
        }

        public static void GrantCommander(float amount)
        {
            if (amount <= 0f) return;
            var player = DataUtil.Instance?.currentPlayer;
            if (player == null) return;
            if (player.level < 1) player.level = 1;
            var state = new CommanderXpState
            {
                Level = player.level,
                CurrentXp = player.commanderExp,
                ExpToNext = player.commanderExpToNext > 0
                    ? player.commanderExpToNext
                    : ProgressionRules.CommanderExpToNext(player.level)
            };
            var before = state.Level;
            state = ProgressionRules.ApplyCommanderXp(state, amount);
            player.level = state.Level;
            player.commanderExp = state.CurrentXp;
            player.commanderExpToNext = state.ExpToNext;
            LastLog ??= new GrantLog();
            LastLog.CommanderLevels += state.LevelsGained;
            LastLog.CommanderLevelAfter = state.Level;
            if (state.LevelsGained > 0)
            {
                LastLog.NotesEn.Add($"Commander Lv.{before} → Lv.{state.Level}");
                LastLog.NotesZh.Add($"舰长 Lv.{before} → Lv.{state.Level}");
            }
        }

        public static void GrantIdleCombat(IList<CardEntity> allCards, HashSet<string> busyIds, float baselineXp)
        {
            if (allCards == null || baselineXp <= 0f) return;
            foreach (var card in allCards)
            {
                if (card == null) continue;
                if (busyIds != null && busyIds.Contains(card.id)) continue;
                ApplyCombat(card, baselineXp * ProgressionRules.IdleCombatShare);
            }
        }

        public static int HighestCombatLevel(IList<CardEntity> cards)
        {
            var highest = 1;
            if (cards == null) return highest;
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (card.Level > highest) highest = card.Level;
            }

            return highest;
        }

        public static int CatchUpCombatLevel(IList<CardEntity> cards = null)
        {
            var owned = cards ?? CardListManager.Instance?.cardEntities;
            var commander = DataUtil.Instance?.currentPlayer?.level ?? 1;
            return ProgressionRules.CatchUpCombatLevel(HighestCombatLevel(owned), Math.Max(1, commander));
        }

        public static void ApplyNewCardDefaults(CardEntity entity, IList<CardEntity> existing = null)
        {
            if (entity == null) return;
            entity.EnsureProgressionDefaults();
            entity.EnergyRank = EnergyRank.F;
            CardEntity.BeginBatchPersist();
            try
            {
                var desired = CatchUpCombatLevel(existing);
                var cap = ProgressionRules.CombatCap(EnergyRank.F);
                var level = Math.Min(Math.Max(1, desired), cap);
                entity.SetLevel(level);
                entity.SetExp(0f);
                entity.ExpToNextLevel = ProgressionRules.CombatExpToNext(level);
                if (desired > cap)
                    entity.storedCombatXp = ProgressionRules.StoredXpForSkippedLevels(cap, desired);
                entity.RefreshBaseAttributes();
                entity.EnsureProgressionDefaults();
            }
            finally
            {
                CardEntity.EndBatchPersist(false);
            }
        }

        public static int WorkshopCorrection()
        {
            ShipService.EnsureReady();
            return ProgressionRules.WorkshopThresholdCorrection(ShipService.GetModuleLevel("mod_armor"));
        }

        public static float TeamPower(IList<CardEntity> members, ProfessionSkill skill, int facilityLevel)
        {
            var levels = new List<int>();
            if (members != null)
            {
                foreach (var card in members)
                {
                    if (card == null) continue;
                    levels.Add(card.GetProfessionLevel(skill));
                }
            }

            return ProgressionRules.TeamPower(levels, facilityLevel);
        }

        public static int BestProfessionLevel(IList<CardEntity> members, ProfessionSkill skill)
        {
            var best = 0;
            if (members == null) return best;
            foreach (var card in members)
            {
                if (card == null) continue;
                var level = card.GetProfessionLevel(skill);
                if (level > best) best = level;
            }

            return best;
        }

        public static bool LeaderMeetsGate(IList<CardEntity> members, ProfessionSkill skill, int requiredLevel)
        {
            return ProgressionRules.LeaderMeetsGate(
                BestProfessionLevel(members, skill), requiredLevel, WorkshopCorrection());
        }

        public static float AdjustedCycleSeconds(int baseSeconds, IList<CardEntity> members, ProfessionSkill skill, int facilityLevel)
        {
            return ProgressionRules.AdjustedCycleSeconds(
                baseSeconds, TeamPower(members, skill, facilityLevel));
        }

        public static EconomyCommandResult SkillGateFailure(
            ProfessionSkill skill, int requiredLevel, IList<CardEntity> members)
        {
            var best = BestProfessionLevel(members, skill);
            var correction = WorkshopCorrection();
            return EconomyCommandResult.Fail(
                UiText.ProductionSkillGate(skill.ToString(), requiredLevel, best, correction));
        }

        public static EconomyCommandResult TryAscendEnergyRank(CardEntity card)
        {
            if (card == null)
                return EconomyCommandResult.Fail("No card selected.");
            card.EnsureProgressionDefaults();
            if (!ProgressionCatalog.TryGetAscensionCost(card.EnergyRank, out var cost))
                return EconomyCommandResult.Fail(UiText.EnergyRankMaxed);

            if ((DataUtil.Instance?.currentPlayer?.creditPoints ?? 0) < cost.CreditQty)
                return EconomyCommandResult.Fail(UiText.EnergyRankNeedCredits(cost.CreditQty));

            var scraps = CountScrap();
            if (scraps < cost.ScrapQty)
                return EconomyCommandResult.Fail(UiText.EnergyRankNeedScrap(cost.ScrapQty));

            if (!ProductionService.TryConsumeLocal(ProgressionCatalog.ScrapDefId, cost.ScrapQty))
                return EconomyCommandResult.Fail(UiText.EnergyRankNeedScrap(cost.ScrapQty));
            if (cost.CreditQty > 0 && !CurrencyService.TrySpendCredits(cost.CreditQty, out var err))
            {
                ProductionService.TryAddLocal(
                    ItemFactory.FromDef(ProgressionCatalog.ScrapDefId, cost.ScrapQty, EconomyConstants.DefaultQuality));
                return EconomyCommandResult.Fail(err);
            }

            card.EnergyRank = cost.To;
            card.DumpStoredCombatXp();
            card.DumpStoredProfessionXp();
            card.EnsureProgressionDefaults();
            card.RefreshBaseAttributes();
            card.NotifyRankAscended();
            DataUtil.Instance?.SaveCardData(CardListManager.Instance?.cardEntities);
            NexusSnackbar.Show(UiText.EnergyRankAscended(card.cardName, cost.To.ToString()));
            return EconomyCommandResult.Ok();
        }

        public static void RefreshDeckScaleUnlocks()
        {
            DeckService.EnsureReady();
            if (DeckService.State == null) return;
            ShipService.EnsureReady();
            var commander = Math.Max(1, DataUtil.Instance?.currentPlayer?.level ?? 1);
            var ship = ShipService.State != null ? Math.Max(1, ShipService.State.level) : 1;
            var chapter1 = ChapterQuestService.IsChapterComplete;
            var dispatch = ShipService.GetModuleLevel("mod_command");
            var auto = ShipService.GetModuleLevel("mod_automation");
            var slots = DeckUnlockTable.ComputeUnlockedSlots(commander, ship, chapter1, false, dispatch);
            var parallel = DeckUnlockTable.ComputeMaxParallel(commander, ship, false, auto);
            var changed = false;
            if (slots > DeckService.State.unlockedDeckSlots)
            {
                DeckService.State.unlockedDeckSlots = slots;
                changed = true;
            }

            if (parallel > DeckService.State.maxParallelActions)
            {
                DeckService.State.maxParallelActions = parallel;
                changed = true;
            }

            if (!changed) return;
            var unlocked = DeckService.State.unlockedDeckSlots;
            if (DeckService.State.decks != null)
            {
                for (var i = 0; i < DeckService.State.decks.Count; i++)
                {
                    if (DeckService.State.decks[i] != null)
                        DeckService.State.decks[i].unlocked = i < unlocked;
                }
            }

            DeckService.Save();
        }

        public static List<OfflineProgressNote> TakeNotes()
        {
            var list = new List<OfflineProgressNote>();
            if (LastLog == null) return list;
            var count = Math.Max(LastLog.NotesEn.Count, LastLog.NotesZh.Count);
            for (var i = 0; i < count; i++)
            {
                list.Add(new OfflineProgressNote
                {
                    textEn = i < LastLog.NotesEn.Count ? LastLog.NotesEn[i] : "",
                    textZh = i < LastLog.NotesZh.Count ? LastLog.NotesZh[i] : ""
                });
            }

            return list;
        }

        public static HashSet<string> BusyCardIds()
        {
            var ids = new HashSet<string>();
            if (!DeckService.IsLoaded) return ids;
            foreach (var deck in DeckService.GetBusyDecks())
            {
                if (deck?.slotCardIds == null) continue;
                foreach (var id in deck.slotCardIds)
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }

            return ids;
        }

        private static void ApplyCombat(CardEntity card, float amount)
        {
            if (card == null || amount <= 0f) return;
            var before = card.Level;
            var atCapBefore = card.Level >= ProgressionRules.CombatCap(card.EnergyRank);
            card.AddExperience(amount);
            if (LastLog == null) return;
            if (card.Level > before)
            {
                LastLog.NotesEn.Add($"{card.cardName} combat Lv.{before} → Lv.{card.Level}");
                LastLog.NotesZh.Add($"{card.cardName} 战斗 Lv.{before} → Lv.{card.Level}");
            }
            else if (!atCapBefore && card.EvolutionPending)
            {
                LastLog.NotesEn.Add($"{card.cardName} combat cap {card.Level} — stored XP {card.storedCombatXp:0}");
                LastLog.NotesZh.Add($"{card.cardName} 战斗等级已达上限 {card.Level}，已储存经验 {card.storedCombatXp:0}");
            }
        }

        private static void ApplyProfession(CardEntity card, ProfessionSkill skill, float amount)
        {
            if (card == null || amount <= 0f) return;
            var before = card.GetProfessionLevel(skill);
            var result = card.AddProfessionExperience(skill, amount);
            if (LastLog == null) return;
            if (result.Level > before)
            {
                LastLog.NotesEn.Add($"{card.cardName} {skill} Lv.{before} → Lv.{result.Level}");
                LastLog.NotesZh.Add($"{card.cardName} {SkillLabelZh(skill)} Lv.{before} → Lv.{result.Level}");
            }

            if (!result.HitMilestone) return;
            var recommended = ProgressionCatalog.RecommendedSpec(skill, result.MilestoneLevel);
            if (skill == ProfessionSkill.Craft || skill == ProfessionSkill.Gather)
            {
                if (card.GetProfessionSpec(skill) == ProfessionSpec.None)
                    card.SetProfessionSpec(skill, recommended);
                LastLog.Specs.Add(new SpecPrompt
                {
                    Card = card,
                    Skill = skill,
                    Milestone = result.MilestoneLevel,
                    Recommended = recommended
                });
            }
        }

        private static void PresentLastLog()
        {
            if (LastLog == null) return;
            if (LastLog.CommanderLeveled)
                NexusSnackbar.Show(UiText.CommanderLeveled(LastLog.CommanderLevelAfter));
            foreach (var spec in LastLog.Specs)
            {
                if (spec?.Card == null) continue;
                var card = spec.Card;
                var skill = spec.Skill;
                var recommended = spec.Recommended;
                NexusSnackbar.Show(UiText.ProfessionMilestone(
                    card.cardName, skill.ToString(), spec.Milestone, recommended.ToString()));
                NexusDialog.Show(
                    UiText.SpecDialogTitle,
                    UiText.SpecDialogBody(card.cardName, skill.ToString(), spec.Milestone, recommended.ToString()),
                    UiText.SpecKeep,
                    null,
                    UiText.SpecOther,
                    () =>
                    {
                        var next = ProgressionCatalog.CycleSpec(card.GetProfessionSpec(skill));
                        card.SetProfessionSpec(skill, next);
                        DataUtil.Instance?.SaveCardData(CardListManager.Instance?.cardEntities);
                        NexusSnackbar.Show(UiText.SpecChanged(card.cardName, next.ToString()));
                    });
            }
        }

        private static void SaveCommander()
        {
            var player = DataUtil.Instance?.currentPlayer;
            if (player != null)
                DataUtil.Instance.SavePlayerData(player);
        }

        private static int CountScrap()
        {
            var total = 0;
            var items = ProductionService.GetLocalItems();
            if (items == null) return 0;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (ItemFactory.ResolveDefId(item) == ProgressionCatalog.ScrapDefId)
                    total += item.quantity;
            }

            return total;
        }

        private static string SkillLabelZh(ProfessionSkill skill) => skill switch
        {
            ProfessionSkill.Gather => "采集",
            ProfessionSkill.Craft => "制造",
            ProfessionSkill.Scan => "扫描",
            ProfessionSkill.Navigate => "航行",
            ProfessionSkill.Logistics => "后勤",
            _ => skill.ToString()
        };
    }
}
