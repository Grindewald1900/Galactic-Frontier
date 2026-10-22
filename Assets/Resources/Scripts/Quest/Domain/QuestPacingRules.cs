using System.Collections.Generic;

namespace Assets.Resources.Scripts.Quest.Domain
{
    /// <summary>
    /// Doc 25 acceptance pacing.
    /// Story and onboarding are linear chains: a later step is offered only after its
    /// prerequisite is satisfied, and steps that are not current stay off the board
    /// until then. The two chains may run at the same time.
    /// Expedition rings on one node are linear. Doc 25 §1 also caps the expedition
    /// track at one Active ring globally, so a second node's ring stays queued.
    /// Loops may run beside the other tracks, up to <see cref="MaxActiveLoops"/>.
    /// Ship gates offer one current gate and may run beside the other tracks.
    /// </summary>
    public static class QuestPacingRules
    {
        public const int MaxActiveStory = 1;
        public const int MaxActiveOnboarding = 1;

        /// <summary>Doc 25 §1: "1 条远征环 Active". Not per node.</summary>
        public const int MaxActiveExpeditions = 1;

        public const int MaxActiveLoops = 2;
        public const int MaxActiveShip = 1;

        public static bool IsLinearChain(QuestType type) =>
            type == QuestType.Story || type == QuestType.Onboarding;

        /// <summary>
        /// Different tracks may be Active together. Inside one track only loops stack,
        /// and only up to <see cref="MaxActiveLoops"/>.
        /// </summary>
        public static bool MayRunBeside(QuestType left, QuestType right)
        {
            if (left == right)
                return left == QuestType.Loop;
            return true;
        }

        /// <summary>
        /// A step that is not the current one stays off the board unless it is already
        /// complete and waiting to be claimed. Loops appear only while they are in the active set.
        /// </summary>
        public static bool StaysOffBoard(QuestType type, bool isCurrent, bool complete)
        {
            if (isCurrent) return false;
            if (type == QuestType.Loop || !complete) return true;
            return false;
        }

        /// <summary>
        /// Picks the single expedition that may be Active or the current Ready ring.
        /// Same-node order is enforced by prerequisites before a ring is triggered.
        /// </summary>
        public static string SelectCurrentExpedition(
            IList<ExpeditionOffer> ordered,
            string routeChoice,
            string convoyRegionId,
            string riftRegionId)
        {
            if (ordered == null || MaxActiveExpeditions < 1) return "";
            string ready = null;
            string actionable = null;
            string preferred = null;
            for (int i = 0; i < ordered.Count; i++)
            {
                var other = ordered[i];
                if (other == null || other.Claimed || !other.Triggered) continue;
                if (!other.Complete)
                {
                    if (actionable == null)
                        actionable = other.QuestId;
                    if (routeChoice == "convoy" && other.RegionId == convoyRegionId)
                        preferred = other.QuestId;
                    if (routeChoice == "rift" && other.RegionId == riftRegionId)
                        preferred = other.QuestId;
                }
                else if (ready == null)
                {
                    ready = other.QuestId;
                }
            }

            return preferred ?? actionable ?? ready ?? "";
        }
    }

    public sealed class ExpeditionOffer
    {
        public string QuestId;
        public string RegionId;
        public bool Claimed;
        public bool Triggered;
        public bool Complete;
    }
}
