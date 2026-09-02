namespace Assets.Resources.Scripts.ChapterQuest.Domain
{
    /// <summary>Flag-only chapter step checks (testable without runtime services).</summary>
    public static class ChapterQuestFlagRules
    {
        public static bool IsStepSatisfiedByFlags(ChapterQuestFlags flags, string stepId)
        {
            if (flags == null || string.IsNullOrEmpty(stepId))
                return false;

            return stepId switch
            {
                ChapterQuestCatalog.StepPrologue => flags.prologueWon,
                ChapterQuestCatalog.StepRecruitCole => flags.emergencyRecruitDone,
                ChapterQuestCatalog.StepFormation => flags.formationReady,
                ChapterQuestCatalog.StepOuterCleanup => flags.outerCleanupWon,
                ChapterQuestCatalog.StepScanSignal => flags.miningSignalScanned,
                ChapterQuestCatalog.StepMiningSpur => flags.miningSpurWon,
                _ => false
            };
        }
    }
}
