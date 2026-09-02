using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.ChapterQuest.Domain
{
    [Serializable]
    public class ChapterQuestFlags
    {
        public bool prologueWon;
        public bool emergencyRecruitDone;
        public bool formationReady;
        public bool outerCleanupWon;
        public bool miningSignalScanned;
        public bool miningSpurWon;
        public bool strategyCompareShown;
    }

    [Serializable]
    public class ChapterQuestState
    {
        public string chapterId = ChapterQuestCatalog.ChapterId;
        public string activeStepId = ChapterQuestCatalog.StepPrologue;
        public List<string> completedStepIds = new List<string>();
        public List<string> claimedStepIds = new List<string>();
        public ChapterQuestFlags flags = new ChapterQuestFlags();
        public bool chapterCompleted;
        public bool chapterSkipped;
        public int count;
    }

    [Serializable]
    public class ChapterQuestRewardGrant
    {
        public string kind = "credits";
        public int credits;
        public string itemDefId = "";
        public int quantity = 1;
        public int quality = 2;
    }

    public sealed class ChapterQuestStepDef
    {
        public string stepId;
        public int order;
        public string titleEn;
        public string titleZh;
        public string hintEn;
        public string hintZh;
        public string targetScreen;
        public string dialogueIdOnStart;
        public string dialogueIdOnClaim;
        public string linkedOnboardingStepId;
        public ChapterQuestRewardGrant[] rewards;
    }

    public enum ChapterQuestStepStatus
    {
        Locked = 0,
        Active = 1,
        Completed = 2
    }

    public sealed class ChapterQuestStepView
    {
        public ChapterQuestStepDef Def;
        public ChapterQuestStepStatus Status;
        public bool CanClaim;
    }

    public sealed class ChapterQuestCommandResult
    {
        public bool Success;
        public string Message = "";

        public static ChapterQuestCommandResult Ok(string message = "") =>
            new ChapterQuestCommandResult { Success = true, Message = message ?? "" };

        public static ChapterQuestCommandResult Fail(string message) =>
            new ChapterQuestCommandResult { Success = false, Message = message ?? "" };
    }
}
