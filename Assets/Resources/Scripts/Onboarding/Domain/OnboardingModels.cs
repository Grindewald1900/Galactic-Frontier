using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Onboarding.Domain
{
    [Serializable]
    public class OnboardingFlags
    {
        public bool formationReady;
        public bool firstBattleWon;
        public bool gatherStartedOrLooted;
        public bool craftedOnce;
        public bool shopTradedOnce;
    }

    [Serializable]
    public class OnboardingState
    {
        public string chainId = OnboardingCatalog.ChainId;
        public string activeStepId = OnboardingCatalog.StepFormation;
        public List<string> completedStepIds = new List<string>();
        public List<string> claimedStepIds = new List<string>();
        public OnboardingFlags flags = new OnboardingFlags();
        public List<string> completedGuideIds = new List<string>();
        public bool chainCompleted;
        public int count;
    }

    [Serializable]
    public class OnboardingRewardGrant
    {
        public string kind = "credits"; // credits | item
        public int credits;
        public string itemDefId = "";
        public int quantity = 1;
        public int quality = 2;
    }

    public sealed class OnboardingStepDef
    {
        public string stepId;
        public int order;
        public string titleEn;
        public string titleZh;
        public string hintEn;
        public string hintZh;
        public string targetScreen; // AppScreen name
        public OnboardingRewardGrant[] rewards;
    }

    public enum OnboardingStepStatus
    {
        Locked = 0,
        Active = 1,
        Completed = 2
    }

    public sealed class OnboardingStepView
    {
        public OnboardingStepDef Def;
        public OnboardingStepStatus Status;
        public bool CanClaim;
    }

    public sealed class OnboardingCommandResult
    {
        public bool Success;
        public string Message = "";

        public static OnboardingCommandResult Ok(string message = "") =>
            new OnboardingCommandResult { Success = true, Message = message ?? "" };

        public static OnboardingCommandResult Fail(string message) =>
            new OnboardingCommandResult { Success = false, Message = message ?? "" };
    }
}
