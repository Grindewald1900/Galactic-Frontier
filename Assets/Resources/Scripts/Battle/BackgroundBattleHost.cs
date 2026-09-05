using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Battle.Domain;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Progression;
using Assets.Resources.Scripts.Progression.Domain;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// DontDestroyOnLoad owner of the headless battle coroutine.
    /// Presentation (BattleScene) pauses this loop and reads/writes <see cref="LiveBattleSession"/>;
    /// leaving BattleScene resumes the coroutine so the fight keeps advancing on Bridge.
    /// </summary>
    public sealed class BackgroundBattleHost : MonoBehaviour
    {
        public static BackgroundBattleHost Instance { get; private set; }

        private const float SecondsPerRound = 0.85f;

        private Coroutine simRoutine;
        private BattleRng rng;
        private bool presentationActive;

        /// <summary>
        /// Fired once when background simulation settles a fight. XP/rewards are granted only at
        /// settlement (not per round), so this is the single point where idle-combat UI should refresh.
        /// </summary>
        public static event Action FinishedTick;

        public static bool IsRunning => Instance != null && Instance.simRoutine != null;

        public static bool IsHandlingAutoCombat =>
            LiveBattleSession.CanResume && LiveBattleSession.IsSpectateAuto;

        public static BackgroundBattleHost Ensure()
        {
            if (Instance != null)
                return Instance;
            var go = new GameObject("BackgroundBattleHost");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<BackgroundBattleHost>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// After load / Bridge return: if combat deck is AutoCombat, ensure a live session
        /// is simulating in the background.
        /// </summary>
        public bool TryBootstrapIdleCombat()
        {
            if (presentationActive)
                presentationActive = false;

            if (IsBattleSceneActive())
                return false;

            if (LiveBattleSession.CanResume)
            {
                StartSimulation();
                PublishLiveStatusLog();
                return true;
            }

            if (!TryCreateSessionFromAutoCombat())
                return false;

            StartSimulation();
            BridgeEventLog.Push(
                BridgeLogCategory.Combat,
                $"Background farm battle started — {LiveBattleSession.RegionId}",
                $"后台挂机战斗已开始 — {LiveBattleSession.RegionId}");
            PublishLiveStatusLog();
            Debug.Log($"[BATTLE-BG] Bootstrapped AutoCombat session region={LiveBattleSession.RegionId}");
            return true;
        }

        /// <summary>BattleScene is driving the fight — stop the headless coroutine.</summary>
        public void AttachPresentation()
        {
            presentationActive = true;
            StopSimulation();
            Debug.Log("[BATTLE-BG] Presentation attached — simulation paused.");
        }

        /// <summary>Clears presentation lock without starting simulation (flee / settled leave).</summary>
        public void ReleasePresentation()
        {
            presentationActive = false;
            StopSimulation();
        }

        /// <summary>
        /// Snapshot is in <see cref="LiveBattleSession"/>; resume headless rounds on MainScene.
        /// </summary>
        public void DetachPresentationAndContinue()
        {
            presentationActive = false;
            if (!LiveBattleSession.CanResume)
            {
                TryBootstrapIdleCombat();
                return;
            }

            SyncRngFromSession();
            StartSimulation();
            PublishLiveStatusLog();
            Debug.Log(
                $"[BATTLE-BG] Detach — simulation running at round {LiveBattleSession.CurrentRound}.");
        }

        public void SyncRngFromSession()
        {
            rng = LiveBattleSession.CreateRngAtCursor();
        }

        public void StartSimulation()
        {
            if (presentationActive || !LiveBattleSession.CanResume)
                return;
            if (simRoutine != null)
                return;
            SyncRngFromSession();
            simRoutine = StartCoroutine(SimulateLoop());
        }

        public void StopSimulation()
        {
            if (simRoutine != null)
            {
                StopCoroutine(simRoutine);
                simRoutine = null;
            }
        }

        private IEnumerator SimulateLoop()
        {
            Debug.Log("[BATTLE-BG] SimulateLoop started.");
            while (LiveBattleSession.CanResume && !presentationActive)
            {
                if (IsBattleSceneActive())
                {
                    yield return null;
                    continue;
                }

                yield return new WaitForSecondsRealtime(SecondsPerRound);

                if (!LiveBattleSession.CanResume || presentationActive || IsBattleSceneActive())
                    continue;

                try
                {
                    SimulateOneRound();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[BATTLE-BG] SimulateOneRound failed: " + ex);
                    break;
                }

                // Keep the internal bridge status line current, but do NOT raise a UI event per
                // round — idle-combat XP/UI updates happen only at settlement (FinishedTick).
                PublishLiveStatusLog();
            }

            simRoutine = null;
            Debug.Log("[BATTLE-BG] SimulateLoop stopped.");
        }

        private static bool IsBattleSceneActive()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene == nameof(SceneLoader.SceneName.BattleScene))
                return true;
            return GameStatusManager.Instance != null
                && GameStatusManager.Instance.CurrentScene == CurrentScene.BATTLE_SCENE;
        }

        private void SimulateOneRound()
        {
            if (!LiveBattleSession.CanResume)
                return;
            rng ??= LiveBattleSession.CreateRngAtCursor();

            var players = LiveBattleSession.Players;
            var enemies = LiveBattleSession.Enemies;
            if (!AnyAlive(players) || !AnyAlive(enemies))
            {
                Finish(BattleOutcomeRules.Resolve(AnyAlive(players), AnyAlive(enemies), false));
                return;
            }

            LiveBattleSession.UpdateRuntime(
                LiveBattleSession.CurrentRound + 1,
                rng.CallCount,
                players,
                enemies);

            if (LiveBattleSession.CurrentRound > LiveBattleSession.MaxRound)
            {
                Finish(BattleOutcomeRules.Resolve(AnyAlive(players), AnyAlive(enemies), true));
                return;
            }

            var order = BuildInitiative(players, enemies);
            foreach (var actor in order)
            {
                if (actor.currentHp <= 0f) continue;
                var foes = actor.isPlayer ? enemies : players;
                var target = PickTarget(actor.isPlayer, foes);
                if (target == null)
                {
                    Finish(BattleOutcomeRules.Resolve(AnyAlive(players), AnyAlive(enemies), false));
                    return;
                }

                float multiplier = 1f;
                if (actor.currentEnergy >= actor.maxEnergy)
                {
                    multiplier = 1.5f;
                    actor.currentEnergy = 0f;
                }
                else
                {
                    actor.currentEnergy = Mathf.Min(actor.maxEnergy, actor.currentEnergy + 30f);
                }

                if (actor.entity == null || target.entity == null)
                    continue;

                var roll = CombatMath.ResolveAttack(
                    actor.entity.Accuracy,
                    target.entity.Dodge,
                    actor.entity.Critical,
                    actor.entity.CriticalDamage,
                    actor.entity.GetBattleAttack(),
                    target.entity.GetBattleDefense(),
                    target.entity.DamageReduction,
                    multiplier,
                    rng);
                if (roll.Hit)
                    target.currentHp = Mathf.Max(0f, target.currentHp - roll.Damage);

                LiveBattleSession.SetRngCalls(rng.CallCount);

                if (!AnyAlive(players) || !AnyAlive(enemies))
                {
                    Finish(BattleOutcomeRules.Resolve(AnyAlive(players), AnyAlive(enemies), false));
                    return;
                }
            }

            LiveBattleSession.UpdateRuntime(
                LiveBattleSession.CurrentRound,
                rng.CallCount,
                players,
                enemies);
        }

        private void Finish(BattleOutcome outcome)
        {
            bool wasSpectate = LiveBattleSession.IsSpectateAuto;
            string regionId = LiveBattleSession.RegionId;
            int round = LiveBattleSession.CurrentRound;

            LiveBattleSession.MarkFinished(outcome);
            ApplySettlement(outcome);

            BridgeEventLog.Push(
                BridgeLogCategory.Combat,
                outcome == BattleOutcome.Victory
                    ? $"Farm battle won (R{round}) — {regionId}"
                    : $"Farm battle lost (R{round}) — {regionId}",
                outcome == BattleOutcome.Victory
                    ? $"挂机战斗胜利（第{round}回合）— {regionId}"
                    : $"挂机战斗失败（第{round}回合）— {regionId}");

            LiveBattleSession.Clear();
            rng = null;
            StopSimulation();
            FinishedTick?.Invoke();
            Debug.Log($"[BATTLE-BG] Fight settled outcome={outcome}.");

            // Continuous AFK: immediately queue the next farm fight while AutoCombat is still running.
            if (wasSpectate && !presentationActive && !IsBattleSceneActive())
                TryBootstrapIdleCombat();
        }

        private static bool TryCreateSessionFromAutoCombat()
        {
            if (!DeckService.IsLoaded)
            {
                if (DataUtil.Instance != null)
                    DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance?.cardEntities);
            }

            if (DataUtil.Instance != null)
                WorldService.EnsureLoaded(DataUtil.Instance);

            if (!DeckService.IsLoaded)
                return false;

            var deck = DeckService.GetActiveCombatDeck();
            if (deck == null || !deck.IsActionBusy || deck.action == null)
                return false;
            if (deck.action.actionType != DeckActionType.AutoCombat)
                return false;
            if (deck.action.status != DeckActionStatus.Running)
                return false;

            string regionId = deck.action.targetId;
            if (string.IsNullOrEmpty(regionId) || !WorldService.IsFarmUnlocked(regionId))
                return false;

            var region = RegionCatalog.Get(regionId);
            if (region == null || string.IsNullOrEmpty(region.farmEncounterId))
                return false;

            var encounter = EncounterCatalog.Get(region.farmEncounterId);
            if (encounter == null)
                return false;

            var cards = CardListManager.Instance?.cardEntities;
            var members = DeckService.GetOrderedMembers(deck.deckId, cards);
            if (members == null || members.Count == 0)
                return false;

            var playerCopies = ListDeepCopyUtil.DeepCopyViaJson(members) ?? new List<CardEntity>();
            var players = new List<LiveCombatantState>();
            for (int i = 0; i < playerCopies.Count; i++)
            {
                var entity = playerCopies[i];
                if (entity == null) continue;
                players.Add(new LiveCombatantState
                {
                    cardId = entity.id ?? "",
                    slotIndex = i,
                    isPlayer = true,
                    currentHp = entity.Health,
                    maxHp = entity.Health,
                    currentEnergy = 0f,
                    maxEnergy = entity.maxEnergy,
                    entity = entity
                });
            }

            var enemyParty = EncounterPartyBuilder.Build(encounter);
            var enemies = new List<LiveCombatantState>();
            for (int i = 0; i < enemyParty.Count; i++)
            {
                var entity = enemyParty[i];
                if (entity == null) continue;
                enemies.Add(new LiveCombatantState
                {
                    cardId = entity.id ?? "",
                    slotIndex = i,
                    isPlayer = false,
                    currentHp = entity.Health,
                    maxHp = entity.Health,
                    currentEnergy = 0f,
                    maxEnergy = entity.maxEnergy,
                    entity = entity
                });
            }

            if (players.Count == 0 || enemies.Count == 0)
                return false;

            long seed = unchecked(regionId.GetHashCode() * 1_000_003L ^ DateTime.UtcNow.Ticks);
            var strategy = CombatStrategyRules.Parse(deck.combatStrategyId);
            LiveBattleSession.Begin(
                seed,
                0,
                0,
                strategy,
                regionId,
                region.farmEncounterId,
                deck.deckId,
                spectateAuto: true,
                prologue: false,
                players,
                enemies);
            return true;
        }

        private static void PublishLiveStatusLog()
        {
            if (!LiveBattleSession.CanResume)
            {
                BridgeEventLog.UpsertLive(
                    "live_bg_battle",
                    BridgeLogCategory.Combat,
                    "No background battle running",
                    "当前无后台战斗");
                return;
            }

            int aliveP = 0;
            int aliveE = 0;
            if (LiveBattleSession.Players != null)
            {
                foreach (var p in LiveBattleSession.Players)
                    if (p != null && p.currentHp > 0f) aliveP++;
            }

            if (LiveBattleSession.Enemies != null)
            {
                foreach (var e in LiveBattleSession.Enemies)
                    if (e != null && e.currentHp > 0f) aliveE++;
            }

            string mode = LiveBattleSession.IsSpectateAuto ? "AutoCombat" : "MainCombat";
            BridgeEventLog.UpsertLive(
                "live_bg_battle",
                BridgeLogCategory.Combat,
                $"{mode} R{LiveBattleSession.CurrentRound} · {LiveBattleSession.RegionId} · allies {aliveP} vs foes {aliveE}",
                $"{mode} 第{LiveBattleSession.CurrentRound}回合 · {LiveBattleSession.RegionId} · 我方{aliveP} vs 敌方{aliveE}");
        }

        private static void ApplySettlement(BattleOutcome outcome)
        {
            var regionId = LiveBattleSession.RegionId;
            var cards = CardListManager.Instance?.cardEntities;
            var deck = DeckService.GetActiveCombatDeck();
            var members = deck != null
                ? DeckService.GetOrderedMembers(deck.deckId, cards)
                : null;

            if (LiveBattleSession.IsSpectateAuto)
            {
                if (members != null)
                {
                    foreach (var m in members)
                    {
                        if (m != null)
                            m.farmWear += WorldConstants.FarmWearPerCycle;
                    }
                }

                if (outcome == BattleOutcome.Victory)
                {
                    Economy.DurabilityService.ApplyCombatWearToEquipped(cards);
                    if (!string.IsNullOrEmpty(regionId))
                        RewardService.GrantForFarmCycle(regionId);
                }

                if (!string.IsNullOrEmpty(regionId))
                    WorldService.MarkFarmTick(regionId);

                float farmCmdLeft = Mathf.Max(
                    0f, ProgressionCatalog.CommanderFarmXp - LiveBattleSession.CommanderXpGranted);
                float farmCombatLeft = Mathf.Max(
                    0f, ProgressionCatalog.FarmCombatXpPerCycle - LiveBattleSession.CombatXpGranted);
                if (farmCmdLeft > 0f || farmCombatLeft > 0f)
                {
                    ProgressionService.BeginGrant();
                    if (farmCombatLeft > 0f)
                        ProgressionService.GrantCombatToParty(members, null, farmCombatLeft);
                    if (farmCmdLeft > 0f)
                        ProgressionService.GrantCommander(farmCmdLeft);
                    ProgressionService.EndGrant(presentUi: false);
                    LiveBattleSession.AddXpGranted(farmCmdLeft, farmCombatLeft);
                }

                if (deck?.action != null)
                {
                    deck.action.lastSettledAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    DeckService.Save();
                }

                DataUtil.Instance?.SaveCardData(cards);
                Debug.Log(
                    $"[BATTLE-BG] Spectate settle outcome={outcome} " +
                    $"cmdLeft={farmCmdLeft:0.#} combatLeft={farmCombatLeft:0.#}");
                return;
            }

            float cmdLeft = Mathf.Max(
                0f, ProgressionCatalog.CommanderBattleXp - LiveBattleSession.CommanderXpGranted);
            float combatLeft = Mathf.Max(
                0f, ProgressionCatalog.MainBattleCombatXp - LiveBattleSession.CombatXpGranted);

            if (outcome == BattleOutcome.Victory)
            {
                if (cmdLeft > 0f || combatLeft > 0f)
                {
                    ProgressionService.BeginGrant();
                    if (combatLeft > 0f)
                        ProgressionService.GrantCombatToParty(members, null, combatLeft);
                    if (cmdLeft > 0f)
                        ProgressionService.GrantCommander(cmdLeft);
                    ProgressionService.EndGrant(presentUi: false);
                    LiveBattleSession.AddXpGranted(cmdLeft, combatLeft);
                }

                if (!LiveBattleSession.IsPrologue && !string.IsNullOrEmpty(regionId))
                {
                    WorldService.EnsureLoaded(DataUtil.Instance);
                    var enc = LiveBattleSession.EncounterId;
                    var result = WorldService.RegisterBattleVictory(regionId, enc);
                    if (result.Success)
                        RewardService.GrantForRegionVictory(regionId, result.WasFirstClear);
                    Economy.DurabilityService.ApplyCombatWearToEquipped(cards);
                }
            }

            DeckService.StopAllMainCombat();
            DataUtil.Instance?.SaveCardData(cards);
        }

        public static void AbortOccupation()
        {
            if (Instance != null)
            {
                Instance.StopSimulation();
                Instance.presentationActive = false;
                Instance.rng = null;
            }

            if (LiveBattleSession.IsSpectateAuto)
            {
                var deck = DeckService.GetActiveCombatDeck();
                if (deck != null && deck.IsActionBusy
                    && deck.action?.actionType == DeckActionType.AutoCombat)
                    DeckService.TryStop(deck.deckId);
            }
            else
            {
                DeckService.StopAllMainCombat();
            }

            LiveBattleSession.Clear();
            BridgeEventLog.UpsertLive(
                "live_bg_battle",
                BridgeLogCategory.Combat,
                "Background battle aborted",
                "后台战斗已中止");
        }

        private static bool AnyAlive(List<LiveCombatantState> list)
        {
            if (list == null) return false;
            foreach (var c in list)
            {
                if (c != null && c.currentHp > 0f)
                    return true;
            }

            return false;
        }

        private static List<LiveCombatantState> BuildInitiative(
            List<LiveCombatantState> players, List<LiveCombatantState> enemies)
        {
            var all = new List<LiveCombatantState>();
            if (players != null) all.AddRange(players);
            if (enemies != null) all.AddRange(enemies);
            all.RemoveAll(c => c == null || c.currentHp <= 0f || c.entity == null);
            all.Sort((a, b) => b.entity.Speed.CompareTo(a.entity.Speed));
            return all;
        }

        private static LiveCombatantState PickTarget(bool attackerIsPlayer, List<LiveCombatantState> foes)
        {
            if (foes == null) return null;
            var alive = new List<LiveCombatantState>();
            foreach (var f in foes)
            {
                if (f != null && f.currentHp > 0f && f.entity != null)
                    alive.Add(f);
            }

            if (alive.Count == 0) return null;
            if (!attackerIsPlayer || alive.Count == 1)
                return alive[0];

            var snaps = new List<CombatantSnapshot>();
            foreach (var t in alive)
            {
                snaps.Add(new CombatantSnapshot
                {
                    Id = t.cardId,
                    CurrentHp = t.currentHp,
                    MaxHp = t.maxHp,
                    Power = t.entity.power,
                    IsPlayer = false
                });
            }

            var pick = CombatStrategyRules.PickEnemyTargetIndex(LiveBattleSession.Strategy, snaps);
            if (pick < 0 || pick >= alive.Count) pick = 0;
            return alive[pick];
        }
    }
}
