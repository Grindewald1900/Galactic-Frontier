using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Battle.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.World.Domain;

namespace Assets.Resources.Scripts.Battle
{
    [Serializable]
    public sealed class LiveCombatantState
    {
        public string cardId = "";
        public int slotIndex;
        public bool isPlayer;
        public float currentHp;
        public float maxHp;
        public float currentEnergy;
        public float maxEnergy;
        public CardEntity entity;
    }

    /// <summary>
    /// Cross-scene battle progress: leave Bridge while fighting continues headlessly,
    /// then restore the same round/HP/energy when re-entering BattleScene.
    /// </summary>
    public static class LiveBattleSession
    {
        public const int MaxRound = 15;

        public static bool Active { get; private set; }
        public static bool Finished { get; private set; }
        public static BattleOutcome Outcome { get; private set; } = BattleOutcome.None;
        public static bool IsSpectateAuto { get; private set; }
        public static bool IsPrologue { get; private set; }
        public static string RegionId { get; private set; } = "";
        public static string EncounterId { get; private set; } = "";
        public static string DeckId { get; private set; } = "";
        public static long Seed { get; private set; }
        public static int RngCalls { get; private set; }
        public static int CurrentRound { get; private set; }
        public static CombatStrategyId Strategy { get; private set; } = CombatStrategyId.Balanced;
        public static List<LiveCombatantState> Players { get; private set; } = new();
        public static List<LiveCombatantState> Enemies { get; private set; } = new();

        /// <summary>Commander XP already granted while this fight ran in the background.</summary>
        public static float CommanderXpGranted { get; private set; }
        /// <summary>Party combat XP already granted while this fight ran in the background.</summary>
        public static float CombatXpGranted { get; private set; }

        public static bool CanResume => Active && !Finished;

        public static void Begin(
            long seed,
            int rngCalls,
            int round,
            CombatStrategyId strategy,
            string regionId,
            string encounterId,
            string deckId,
            bool spectateAuto,
            bool prologue,
            List<LiveCombatantState> players,
            List<LiveCombatantState> enemies)
        {
            Active = true;
            Finished = false;
            Outcome = BattleOutcome.None;
            Seed = seed;
            RngCalls = Math.Max(0, rngCalls);
            CurrentRound = Math.Max(0, round);
            Strategy = strategy;
            RegionId = regionId ?? "";
            EncounterId = encounterId ?? "";
            DeckId = deckId ?? "";
            IsSpectateAuto = spectateAuto;
            IsPrologue = prologue;
            Players = players ?? new List<LiveCombatantState>();
            Enemies = enemies ?? new List<LiveCombatantState>();
            CommanderXpGranted = 0f;
            CombatXpGranted = 0f;
        }

        public static void UpdateRuntime(
            int round,
            int rngCalls,
            List<LiveCombatantState> players,
            List<LiveCombatantState> enemies)
        {
            if (!Active) return;
            CurrentRound = round;
            RngCalls = Math.Max(0, rngCalls);
            if (players != null) Players = players;
            if (enemies != null) Enemies = enemies;
        }

        public static void AddXpGranted(float commanderXp, float combatXp)
        {
            if (!Active) return;
            CommanderXpGranted = Math.Max(0f, CommanderXpGranted + commanderXp);
            CombatXpGranted = Math.Max(0f, CombatXpGranted + combatXp);
        }

        public static void MarkFinished(BattleOutcome outcome)
        {
            Finished = true;
            Outcome = outcome;
            Active = true;
        }

        public static void Clear()
        {
            Active = false;
            Finished = false;
            Outcome = BattleOutcome.None;
            IsSpectateAuto = false;
            IsPrologue = false;
            RegionId = "";
            EncounterId = "";
            DeckId = "";
            Seed = 0;
            RngCalls = 0;
            CurrentRound = 0;
            Strategy = CombatStrategyId.Balanced;
            Players = new List<LiveCombatantState>();
            Enemies = new List<LiveCombatantState>();
            CommanderXpGranted = 0f;
            CombatXpGranted = 0f;
        }

        public static BattleRng CreateRngAtCursor()
        {
            var rng = new BattleRng(Seed);
            rng.Burn(RngCalls);
            return rng;
        }

        public static void SetRngCalls(int calls) => RngCalls = Math.Max(0, calls);
    }
}
