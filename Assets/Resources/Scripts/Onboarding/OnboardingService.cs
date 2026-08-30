using System.Collections.Generic;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Utils;
using UnityEngine;

namespace Assets.Resources.Scripts.Onboarding
{
    /// <summary>Linear onboarding_v1 chain (systems/10). Flags set by gameplay; claim advances steps.</summary>
    public static class OnboardingService
    {
        public static OnboardingState State { get; private set; }
        public static bool IsLoaded => State != null;

        public static void Clear() => State = null;

        public static void EnsureLoaded(DataUtil dataUtil = null)
        {
            var util = dataUtil ?? DataUtil.Instance;
            if (util == null)
            {
                State ??= CreateDefault();
                return;
            }

            if (State != null) return;

            var loaded = util.LoadOnboardingState();
            State = loaded ?? CreateDefault();
            if (loaded == null)
                util.SaveOnboardingState(State, touchMeta: false);

            Evaluate(persist: true);
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null) return;
            var util = dataUtil ?? DataUtil.Instance;
            util?.SaveOnboardingState(State, touchMeta: true);
        }

        public static bool IsChainComplete
        {
            get
            {
                EnsureLoaded();
                return State.chainCompleted;
            }
        }

        public static OnboardingStepDef ActiveStep
        {
            get
            {
                EnsureLoaded();
                if (State.chainCompleted) return null;
                return OnboardingCatalog.Get(State.activeStepId);
            }
        }

        public static List<OnboardingStepView> GetStepViews()
        {
            EnsureLoaded();
            Evaluate(persist: false);
            var list = new List<OnboardingStepView>();
            foreach (var def in OnboardingCatalog.All)
            {
                if (def == null) continue;
                var status = ResolveStatus(def.stepId);
                list.Add(new OnboardingStepView
                {
                    Def = def,
                    Status = status,
                    CanClaim = status == OnboardingStepStatus.Active && IsConditionMet(def.stepId)
                               && !IsClaimed(def.stepId)
                });
            }

            return list;
        }

        public static OnboardingCommandResult TryClaim(string stepId)
        {
            EnsureLoaded();
            Evaluate(persist: false);

            var def = OnboardingCatalog.Get(stepId);
            if (def == null)
                return OnboardingCommandResult.Fail("Unknown step.");
            if (State.chainCompleted)
                return OnboardingCommandResult.Fail("Onboarding already complete.");
            if (State.activeStepId != stepId)
                return OnboardingCommandResult.Fail("Step is not active.");
            if (IsClaimed(stepId))
                return OnboardingCommandResult.Fail("Already claimed.");
            if (!IsConditionMet(stepId))
                return OnboardingCommandResult.Fail("Conditions not met yet.");

            if (!GrantRewards(def))
                return OnboardingCommandResult.Fail("Could not grant rewards (warehouse full?).");

            if (!State.claimedStepIds.Contains(stepId))
                State.claimedStepIds.Add(stepId);
            if (!State.completedStepIds.Contains(stepId))
                State.completedStepIds.Add(stepId);

            var next = OnboardingCatalog.NextAfter(stepId);
            if (next == null)
            {
                State.chainCompleted = true;
                State.activeStepId = "";
            }
            else
            {
                State.activeStepId = next.stepId;
            }

            Save();
            Assets.Resources.Scripts.Unlock.FeatureUnlockService.Evaluate();
            return OnboardingCommandResult.Ok(next == null
                ? "Onboarding complete."
                : $"Claimed {stepId}; next {next.stepId}");
        }

        public static void NotifyFormationReady()
        {
            EnsureLoaded();
            if (State.flags == null) State.flags = new OnboardingFlags();
            if (State.flags.formationReady) return;
            State.flags.formationReady = true;
            Save();
        }

        public static void NotifyFirstBattleWon()
        {
            EnsureLoaded();
            if (State.flags == null) State.flags = new OnboardingFlags();
            if (State.flags.firstBattleWon) return;
            State.flags.firstBattleWon = true;
            Save();
        }

        public static void NotifyGatherProgress()
        {
            EnsureLoaded();
            if (State.flags == null) State.flags = new OnboardingFlags();
            if (State.flags.gatherStartedOrLooted) return;
            State.flags.gatherStartedOrLooted = true;
            Save();
        }

        public static void NotifyCraftedOnce()
        {
            EnsureLoaded();
            if (State.flags == null) State.flags = new OnboardingFlags();
            if (State.flags.craftedOnce) return;
            State.flags.craftedOnce = true;
            Save();
        }

        public static void NotifyShopTraded()
        {
            EnsureLoaded();
            if (State.flags == null) State.flags = new OnboardingFlags();
            if (State.flags.shopTradedOnce) return;
            State.flags.shopTradedOnce = true;
            Save();
        }

        /// <summary>Recompute auto-detectable flags from live game state.</summary>
        public static void Evaluate(bool persist = true)
        {
            EnsureLoaded();
            if (State.flags == null) State.flags = new OnboardingFlags();

            var changed = false;
            if (!State.flags.formationReady && HasFormationMember())
            {
                State.flags.formationReady = true;
                changed = true;
            }

            if (!State.flags.gatherStartedOrLooted && HasGatherLoot())
            {
                State.flags.gatherStartedOrLooted = true;
                changed = true;
            }

            if (changed && persist)
                Save();
        }

        private static OnboardingState CreateDefault() =>
            new OnboardingState
            {
                chainId = OnboardingCatalog.ChainId,
                activeStepId = OnboardingCatalog.StepFormation,
                completedStepIds = new List<string>(),
                claimedStepIds = new List<string>(),
                flags = new OnboardingFlags(),
                chainCompleted = false
            };

        private static bool IsClaimed(string stepId) =>
            State?.claimedStepIds != null && State.claimedStepIds.Contains(stepId);

        private static OnboardingStepStatus ResolveStatus(string stepId)
        {
            if (State.completedStepIds != null && State.completedStepIds.Contains(stepId))
                return OnboardingStepStatus.Completed;
            if (State.chainCompleted)
                return OnboardingStepStatus.Completed;
            if (State.activeStepId == stepId)
                return OnboardingStepStatus.Active;
            return OnboardingStepStatus.Locked;
        }

        private static bool IsConditionMet(string stepId)
        {
            var f = State.flags ?? new OnboardingFlags();
            return stepId switch
            {
                OnboardingCatalog.StepFormation => f.formationReady || HasFormationMember(),
                OnboardingCatalog.StepFirstBattle => f.firstBattleWon,
                OnboardingCatalog.StepGather => f.gatherStartedOrLooted || HasGatherLoot(),
                OnboardingCatalog.StepCraft => f.craftedOnce,
                OnboardingCatalog.StepNpcShop => f.shopTradedOnce,
                _ => false
            };
        }

        private static bool HasFormationMember()
        {
            if (!DeckService.IsLoaded) return false;
            foreach (var deck in DeckService.GetDecks())
            {
                if (deck != null && deck.unlocked && deck.MemberCount >= 1)
                    return true;
            }

            return false;
        }

        private static bool HasGatherLoot()
        {
            var items = ProductionService.GetLocalItems();
            if (items == null) return false;
            foreach (var node in GatherNodeCatalog.All)
            {
                if (node == null || string.IsNullOrEmpty(node.outputDefId)) continue;
                if (InventoryRules.CountOf(ItemFactory.ToStacks(items), node.outputDefId, 1) > 0)
                    return true;
            }

            return false;
        }

        private static bool GrantRewards(OnboardingStepDef def)
        {
            if (def?.rewards == null) return true;
            foreach (var reward in def.rewards)
            {
                if (reward == null) continue;
                if (reward.kind == "credits" || reward.credits > 0)
                {
                    if (reward.credits > 0)
                        CurrencyService.AddCredits(reward.credits);
                    continue;
                }

                if (reward.kind == "item" && !string.IsNullOrEmpty(reward.itemDefId))
                {
                    var item = ItemFactory.FromDef(
                        reward.itemDefId,
                        Mathf.Max(1, reward.quantity),
                        reward.quality > 0 ? reward.quality : EconomyConstants.DefaultQuality);
                    if (!ProductionService.TryAddLocal(item))
                    {
                        IdleSettlementService.EnsureLoaded();
                        // Fallback: leave message; caller fails claim so player can free space.
                        Debug.LogWarning("[ONBOARD] Reward item grant failed (warehouse full): " + reward.itemDefId);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
