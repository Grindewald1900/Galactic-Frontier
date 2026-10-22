using System.Collections.Generic;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Onboarding
{
    /// <summary>Facade over QuestService for onboarding_v1 (doc 17 / 25).</summary>
    public static class OnboardingService
    {
        public static OnboardingState State { get; private set; }
        public static bool IsLoaded => State != null;

        public static void Bind(OnboardingState state) => State = state;

        public static void Clear() => State = null;

        public static void EnsureLoaded(DataUtil dataUtil = null) =>
            QuestService.EnsureLoaded(dataUtil);

        public static void Save(DataUtil dataUtil = null) =>
            QuestService.Save(dataUtil);

        public static bool IsChainComplete
        {
            get
            {
                EnsureLoaded();
                return State != null && State.chainCompleted;
            }
        }

        public static OnboardingStepDef ActiveStep
        {
            get
            {
                EnsureLoaded();
                if (State == null || State.chainCompleted) return null;
                return OnboardingCatalog.Get(State.activeStepId);
            }
        }

        public static List<OnboardingStepView> GetStepViews()
        {
            EnsureLoaded();
            var list = new List<OnboardingStepView>();
            foreach (var view in QuestService.GetViews(QuestType.Onboarding))
            {
                if (view?.Def == null) continue;
                var def = OnboardingCatalog.Get(view.Def.questId);
                if (def == null) continue;
                list.Add(new OnboardingStepView
                {
                    Def = def,
                    Status = MapStatus(view.Status),
                    CanClaim = view.CanClaim
                });
            }

            return list;
        }

        public static OnboardingCommandResult TryClaim(string stepId)
        {
            var r = QuestService.TryClaim(stepId);
            return r.Success
                ? OnboardingCommandResult.Ok(r.Message)
                : OnboardingCommandResult.Fail(r.Message);
        }

        public static void NotifyFormationReady() => QuestService.NotifyFormationReady();
        public static void NotifyFirstBattleWon() => QuestService.NotifyFirstBattleWon();
        public static void NotifyGatherProgress() => QuestService.NotifyGatherProgress();
        public static void NotifyCraftedOnce(string outputDefId = "") =>
            QuestService.NotifyCraftedOnce(outputDefId);
        public static void NotifyShopTraded() => QuestService.NotifyShopTraded();

        public static void Evaluate(bool persist = true) =>
            QuestService.Evaluate(persist);

        private static OnboardingStepStatus MapStatus(QuestStatus status) =>
            status == QuestStatus.Claimed
                ? OnboardingStepStatus.Completed
                : status == QuestStatus.Locked
                    ? OnboardingStepStatus.Locked
                    : OnboardingStepStatus.Active;
    }
}
