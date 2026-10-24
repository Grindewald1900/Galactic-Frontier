using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.World.Domain;

namespace Assets.Resources.Scripts.Quest.Domain
{
    public enum QuestType
    {
        Onboarding = 0,
        Story = 1,
        Expedition = 2,
        Loop = 3,
        Ship = 4
    }

    public enum QuestStatus
    {
        Locked = 0,
        Active = 1,
        Ready = 2,
        Claimed = 3
    }

    public enum QuestLoopKind
    {
        None = 0,
        Farm = 1,
        Gather = 2,
        Shop = 3,
        Craft = 4
    }

    [Serializable]
    public class QuestRewardGrant
    {
        public string kind = "credits";
        public int credits;
        public string itemDefId = "";
        public int quantity = 1;
        public int quality = 2;
    }

    [Serializable]
    public class QuestLoopProgress
    {
        public string questId = "";
        public int count;
    }

    [Serializable]
    public class QuestState
    {
        public string schema = "quest_v1";
        public List<string> claimedIds = new List<string>();
        public List<string> completedIds = new List<string>();
        public List<string> completedGuideIds = new List<string>();
        public List<string> playedDialogueIds = new List<string>();
        public List<QuestLoopProgress> loops = new List<QuestLoopProgress>();
        public List<string> activeLoopIds = new List<string>();
        public string activeOnboardingId = "ob_formation";
        public string activeStoryId = "ch1_prologue";
        public string routeChoice = "";
        public bool onboardingComplete;
        public bool chapterComplete;
        public bool chapterSkipped;
        public QuestFlags flags = new QuestFlags();
        public int count;
    }

    [Serializable]
    public class QuestFlags
    {
        public bool formationReady;
        public bool firstBattleWon;
        public bool gatherStartedOrLooted;
        public bool craftedOnce;
        public bool shopTradedOnce;
        public bool prologueWon;
        public bool emergencyRecruitDone;
        public bool chapterFormationReady;
        /// <summary>Player has entered a battle while Patchwork Fleet was the current story quest.</summary>
        public bool formationLessonSeen;
        /// <summary>Player moved Cole into the front row after <see cref="formationLessonSeen"/>.</summary>
        public bool coleFrontAdjusted;
        public bool outerCleanupWon;
        public bool miningSignalScanned;
        public bool miningSpurWon;
        public bool strategyCompareShown;
        public bool gatherCycleSettled;
        public bool heldRepairKit;
        public bool routeChoiceMade;
        public bool pendingLootClaimed;
        public bool frontierBossWon;
    }

    public sealed class QuestDef
    {
        public string questId;
        public string displayId;
        public QuestType type;
        public int order;
        public string titleEn;
        public string titleZh;
        public string hintEn;
        public string hintZh;
        public string targetScreen;
        public string[] dependsOn;
        public string regionId = "";
        public string bodyId = "";
        public int ring;
        public QuestLoopKind loopKind;
        public int loopTarget;
        public string gatherNodeId = "";
        public string linkedOnboardingStepId = "";
        public string dialogueIdOnStart = "";
        public string dialogueIdOnClaim = "";
        public QuestRewardGrant[] rewards;
    }

    public sealed class QuestView
    {
        public QuestDef Def;
        public QuestStatus Status;
        public bool CanClaim;
        public int LoopCount;
        public int LoopTarget;
    }

    public sealed class QuestCommandResult
    {
        public bool Success;
        public string Message = "";

        public static QuestCommandResult Ok(string message = "") =>
            new QuestCommandResult { Success = true, Message = message ?? "" };

        public static QuestCommandResult Fail(string message) =>
            new QuestCommandResult { Success = false, Message = message ?? "" };
    }

    public static class QuestIds
    {
        public const string ObFormation = "ob_formation";
        public const string ObFirstBattle = "ob_first_battle";
        public const string ObGather = "ob_gather";
        public const string ObCraft = "ob_craft";
        public const string ObNpcShop = "ob_npc_shop";

        public const string ChPrologue = "ch1_prologue";
        public const string ChRecruitCole = "ch1_recruit_cole";
        public const string ChFormation = "ch1_formation";
        public const string ChOuterCleanup = "ch1_outer_cleanup";
        public const string ChScanSignal = "ch1_scan_signal";
        public const string ChMiningSpur = "ch1_mining_spur";
        public const string ChIdleGather = "ch1_idle_gather";
        public const string ChHullLeak = "ch1_hull_leak";
        public const string ChTwoRoutes = "ch1_two_routes";
        public const string ChExpeditionPrep = "ch1_expedition_prep";
        public const string ChOpenTheLock = "ch1_open_the_lock";

        public const string ExOuterR1 = "ex_outer_r1";
        public const string ExOuterR2 = "ex_outer_r2";
        public const string ExOuterR3 = "ex_outer_r3";
        public const string ExMiningR1 = "ex_mining_r1";
        public const string ExMiningR2 = "ex_mining_r2";
        public const string ExMiningR3 = "ex_mining_r3";
        public const string ExRiftR1 = "ex_rift_r1";
        public const string ExRiftR2 = "ex_rift_r2";
        public const string ExRiftR3 = "ex_rift_r3";
        public const string ExAbyssR1 = "ex_abyss_r1";
        public const string ExAbyssR2 = "ex_abyss_r2";
        public const string ExAbyssR3 = "ex_abyss_r3";
        public const string ExConvoyR1 = "ex_convoy_r1";
        public const string ExConvoyR2 = "ex_convoy_r2";
        public const string ExConvoyR3 = "ex_convoy_r3";
        public const string ExAnchorR1 = "ex_anchor_r1";
        public const string ExAnchorR2 = "ex_anchor_r2";
        public const string ExAnchorR3 = "ex_anchor_r3";

        public const string ShRangeCargo = "sh_range_cargo";
        public const string ShRiftGate = "sh_rift_gate";
        public const string ShAbyssGate = "sh_abyss_gate";
        public const string ShAnchorGate = "sh_anchor_gate";

        public const string LpFarmOuter = "lp_farm_outer";
        public const string LpGatherOuter = "lp_gather_outer";
        public const string LpFarmMining = "lp_farm_mining";
        public const string LpGatherMining = "lp_gather_mining";
        public const string LpFarmRift = "lp_farm_rift";
        public const string LpGatherRift = "lp_gather_rift";
        public const string LpFarmAbyss = "lp_farm_abyss";
        public const string LpGatherAbyss = "lp_gather_abyss";
        public const string LpFarmConvoy = "lp_farm_convoy";
        public const string LpGatherConvoy = "lp_gather_convoy";
        public const string LpFarmAnchor = "lp_farm_anchor";
        public const string LpShop = "lp_shop_restock";
        public const string LpCraftRepair = "lp_craft_repair";

        public const string BodyOuter = "body_outer_haven";
        public const string BodyMining = "body_mining_spur";
        public const string BodyRift = "body_quantum_rift";
        public const string BodyAbyss = "body_abyssal_edge";
        public const string BodyConvoy = "body_convoy_lane";
        public const string BodyAnchor = "body_frontier_anchor";

        public static string RegionForNode(string node) => node switch
        {
            "outer" => WorldConstants.OuterBeltId,
            "mining" => WorldConstants.MiningSpurId,
            "rift" => WorldConstants.QuantumRiftId,
            "abyss" => WorldConstants.AbyssalEdgeId,
            "convoy" => WorldConstants.ConvoyLaneId,
            "anchor" => WorldConstants.FrontierBossId,
            _ => ""
        };

        public static string BodyForNode(string node) => node switch
        {
            "outer" => BodyOuter,
            "mining" => BodyMining,
            "rift" => BodyRift,
            "abyss" => BodyAbyss,
            "convoy" => BodyConvoy,
            "anchor" => BodyAnchor,
            _ => ""
        };
    }
}
