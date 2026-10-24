using System;
using GalacticFrontier.DebugTools;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GalacticFrontier.DebugTools
{
    static class GfDebugMcp
    {
        public static object Call(Func<GfDebugResult> action)
        {
            try
            {
                var result = action();
                return result.Success
                    ? new SuccessResponse(result.Message, result.Data)
                    : new ErrorResponse(result.Message, result.Data);
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static string Text(JObject args, string key)
        {
            if (args == null || args[key] == null || args[key].Type == JTokenType.Null)
                return "";
            return args[key].ToString();
        }

        public static int Int(JObject args, string key, int fallback)
        {
            if (args == null || args[key] == null || args[key].Type == JTokenType.Null)
                return fallback;
            return args[key].Type == JTokenType.Integer
                ? args[key].Value<int>()
                : int.Parse(args[key].ToString());
        }
    }

    [McpForUnityTool("gf_query_quest", Description = "Read the current main quest, whether its conditions are met, and feature unlocks. Read-only. Requires Play Mode.")]
    public static class GfQueryQuestTool
    {
        public static object HandleCommand(JObject @params) => GfDebugMcp.Call(GfDebugSession.QueryQuest);
    }

    [McpForUnityTool("gf_new_player", Description = "Create a new player through DataUtil.CreatePlayerData and load MainScene. Writes only to the sandbox save folder.")]
    public static class GfNewPlayerTool
    {
        public static object HandleCommand(JObject @params) => GfDebugMcp.Call(GfDebugSession.NewPlayer);
    }

    [McpForUnityTool("gf_jump_quest", Description = "Make a catalog quest the current target. Prerequisites are completed and claimed through QuestService. The target itself is not claimed.")]
    public static class GfJumpQuestTool
    {
        public class Parameters
        {
            [ToolParameter("Quest id, for example ch1_recruit_cole.", Required = true)]
            public string quest_id { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            GfDebugMcp.Call(() => GfDebugSession.JumpToQuest(GfDebugMcp.Text(@params, "quest_id")));
    }

    [McpForUnityTool("gf_jump_battle", Description = "Enter a real battle for an encounter id by reusing ExploreScreen.LaunchBattle or the prologue battle entry.")]
    public static class GfJumpBattleTool
    {
        public class Parameters
        {
            [ToolParameter("Encounter id, for example enc_outer_boss.", Required = true)]
            public string encounter_id { get; set; }

            [ToolParameter("Region id. Optional when the encounter belongs to one campaign.", Required = false)]
            public string region_id { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            GfDebugMcp.Call(() => GfDebugSession.JumpToBattle(
                GfDebugMcp.Text(@params, "encounter_id"),
                GfDebugMcp.Text(@params, "region_id")));
    }

    [McpForUnityTool("gf_set_deck_slot", Description = "Write one combat-deck slot through DeckService. Empty slots stay empty. card_id empty clears the slot.")]
    public static class GfSetDeckSlotTool
    {
        public class Parameters
        {
            [ToolParameter("Zero-based slot index. The combat deck has 5 slots.", Required = true)]
            public int slot_index { get; set; }

            [ToolParameter("Owned card instance id. Empty string clears the slot.", Required = true)]
            public string card_id { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            GfDebugMcp.Call(() => GfDebugSession.SetDeckSlot(
                GfDebugMcp.Int(@params, "slot_index", -1),
                GfDebugMcp.Text(@params, "card_id")));
    }

    [McpForUnityTool("gf_grant_resource", Description = "Add a warehouse item through ProductionService and/or credits through CurrencyService.")]
    public static class GfGrantResourceTool
    {
        public class Parameters
        {
            [ToolParameter("Item definition id, for example mat_scrap.", Required = false)]
            public string item_id { get; set; }

            [ToolParameter("Item quantity. Required when item_id is set.", Required = false, DefaultValue = "1")]
            public int quantity { get; set; } = 1;

            [ToolParameter("Credits to add. Zero skips credits.", Required = false, DefaultValue = "0")]
            public int credits { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            GfDebugMcp.Call(() => GfDebugSession.GrantResource(
                GfDebugMcp.Text(@params, "item_id"),
                GfDebugMcp.Int(@params, "quantity", 1),
                GfDebugMcp.Int(@params, "credits", 0)));
    }

    [McpForUnityTool("gf_set_stage", Description = "Advance a region's campaign to a 1-based cleared stage through WorldService.RegisterBattleVictory and RewardService. 0 leaves progress unchanged.")]
    public static class GfSetStageTool
    {
        public class Parameters
        {
            [ToolParameter("Region id, for example sec01_outer_belt.", Required = true)]
            public string region_id { get; set; }

            [ToolParameter("1-based highest cleared stage to reach.", Required = true)]
            public int stage_index { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            GfDebugMcp.Call(() => GfDebugSession.SetStage(
                GfDebugMcp.Text(@params, "region_id"),
                GfDebugMcp.Int(@params, "stage_index", -1)));
    }

    [McpForUnityTool("gf_open_screen", Description = "Open a real Nexus or battle page in Play Mode. screen_id: bridge, explore, formation, missions, ship, recruit, recruit_result, battle, battle_report, characters, cards, inventory, crafting, market, settings, production_lines, debug. Does not write the official save.")]
    public static class GfOpenScreenTool
    {
        public class Parameters
        {
            [ToolParameter("Page id. explore is the star map. battle is BattleScene. battle_report is the existing settlement panel. recruit_result presses Pull One.", Required = true)]
            public string screen_id { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            GfDebugMcp.Call(() => GfDebugSession.OpenScreen(GfDebugMcp.Text(@params, "screen_id")));
    }

    [McpForUnityTool("gf_capture_screen", Description = "Open a page, wait for its UI, then capture the Game view including overlay canvases. Writes a PNG under Temp/gf-ui-shots and returns its absolute path. Optional width and height resize the Game view.")]
    public static class GfCaptureScreenTool
    {
        public class Parameters
        {
            [ToolParameter("Page id. Same ids as gf_open_screen.", Required = true)]
            public string screen_id { get; set; }

            [ToolParameter("Game view width in pixels. Optional. Must be paired with height.", Required = false)]
            public int width { get; set; }

            [ToolParameter("Game view height in pixels. Optional. Must be paired with width.", Required = false)]
            public int height { get; set; }
        }

        public static object HandleCommand(JObject @params) =>
            GfDebugMcp.Call(() => GfDebugSession.CaptureScreen(
                GfDebugMcp.Text(@params, "screen_id"),
                GfDebugMcp.Int(@params, "width", 0),
                GfDebugMcp.Int(@params, "height", 0)));
    }

    public sealed class GfDebugWindow : EditorWindow
    {
        private string questId = "ch1_recruit_cole";
        private string encounterId = "enc_outer_boss";
        private string regionId = "sec01_outer_belt";
        private int slotIndex = 4;
        private string cardId = "";
        private string itemId = "mat_scrap";
        private int quantity = 5;
        private int credits;
        private int stageIndex = 1;
        private string screenId = "bridge";
        private int shotWidth = 1600;
        private int shotHeight = 900;
        private string lastResult = "";

        [MenuItem("Tools/Galactic Frontier/Debug/Open Debug Window")]
        public static void Open() => GetWindow<GfDebugWindow>("GF Debug");

        [MenuItem("Tools/Galactic Frontier/Debug/Query Quest State")]
        public static void MenuQuery() => Debug.Log(JsonConvert.SerializeObject(GfDebugSession.QueryQuest(), Formatting.Indented));

        [MenuItem("Tools/Galactic Frontier/Debug/New Player (Sandbox)")]
        public static void MenuNewPlayer() => Debug.Log(JsonConvert.SerializeObject(GfDebugSession.NewPlayer(), Formatting.Indented));

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sandbox saves go to Temp/gf-debug-saves. Play Mode is required.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Query quest"))
                Show(GfDebugSession.QueryQuest());
            if (GUILayout.Button("New player (sandbox)"))
                Show(GfDebugSession.NewPlayer());

            questId = EditorGUILayout.TextField("Quest id", questId);
            if (GUILayout.Button("Jump to quest"))
                Show(GfDebugSession.JumpToQuest(questId));

            encounterId = EditorGUILayout.TextField("Encounter id", encounterId);
            regionId = EditorGUILayout.TextField("Region id", regionId);
            if (GUILayout.Button("Jump to battle"))
                Show(GfDebugSession.JumpToBattle(encounterId, regionId));

            slotIndex = EditorGUILayout.IntField("Slot index", slotIndex);
            cardId = EditorGUILayout.TextField("Card id", cardId);
            if (GUILayout.Button("Set deck slot"))
                Show(GfDebugSession.SetDeckSlot(slotIndex, cardId));

            itemId = EditorGUILayout.TextField("Item id", itemId);
            quantity = EditorGUILayout.IntField("Quantity", quantity);
            credits = EditorGUILayout.IntField("Credits", credits);
            if (GUILayout.Button("Grant resource"))
                Show(GfDebugSession.GrantResource(itemId, quantity, credits));

            stageIndex = EditorGUILayout.IntField("Stage index", stageIndex);
            if (GUILayout.Button("Set stage"))
                Show(GfDebugSession.SetStage(regionId, stageIndex));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pages: bridge, explore, formation, missions, ship, recruit, recruit_result, battle, battle_report.", EditorStyles.wordWrappedLabel);
            screenId = EditorGUILayout.TextField("Screen id", screenId);
            shotWidth = EditorGUILayout.IntField("Shot width", shotWidth);
            shotHeight = EditorGUILayout.IntField("Shot height", shotHeight);
            if (GUILayout.Button("Open screen"))
                Show(GfDebugSession.OpenScreen(screenId));
            if (GUILayout.Button("Capture screen"))
                Show(GfDebugSession.CaptureScreen(screenId, shotWidth, shotHeight));

            EditorGUILayout.LabelField("Last result", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(lastResult, GUILayout.MinHeight(120f));
        }

        private void Show(GfDebugResult result)
        {
            lastResult = JsonConvert.SerializeObject(result, Formatting.Indented);
            Debug.Log(lastResult);
        }
    }
}
