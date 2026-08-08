using Assets.Scripts.Utils;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// English-default UI copy with Simplified Chinese translations for the AppShell rewrite.
    /// </summary>
    internal static class UiText
    {
        public static string T(string english, string zhCn) => LocalizationUtil.T(english, zhCn);

        // Shell / navigation
        public static string ScreenBridge => T("Bridge", "舰桥");
        public static string ScreenBattle => T("Explore", "探索");
        public static string ScreenFormation => T("Formation", "编队");
        public static string ScreenCharacters => T("Characters", "角色");
        public static string ScreenCards => T("Cards", "卡牌");
        public static string ScreenInventory => T("Inventory", "仓库");
        public static string ScreenCrafting => T("Crafting", "制造");
        public static string ScreenMarket => T("Market", "市场");
        public static string ScreenMissions => T("Missions", "任务");
        public static string ScreenSettings => T("Settings", "设置");

        public static string Breadcrumb(AppScreen screen) => screen switch
        {
            AppScreen.Bridge => T("Bridge", "舰桥"),
            AppScreen.Battle => T("Explore / Card Battle", "探索 / 卡牌战斗"),
            AppScreen.Formation => T("Formation", "编队"),
            AppScreen.Characters => T("Characters", "角色"),
            AppScreen.Cards => T("Cards", "卡牌"),
            AppScreen.Inventory => T("Inventory", "仓库"),
            AppScreen.Crafting => T("Crafting", "制造"),
            AppScreen.Market => T("Market", "市场"),
            AppScreen.Missions => T("Missions", "任务"),
            AppScreen.Settings => T("Settings", "设置"),
            _ => screen.ToString()
        };

        public static string StatusShortcuts => T(
            "F1 Bridge  F2 Explore  F3 Formation  F4 Characters  F5 Inventory  F6 Crafting  F7 Market  F8 Missions",
            "F1 舰桥  F2 探索  F3 编队  F4 角色  F5 仓库  F6 制造  F7 市场  F8 任务");

        public static string StatusVersion => T(
            "NEXUS COMMAND · Fully automatic combat",
            "NEXUS COMMAND · 全自动战斗");

        public static string MainMenuTagline => T(
            "FULL UI REWRITE · AUTO COMBAT",
            "界面重写 · 全自动战斗");

        // Bridge
        public static string BridgeTitle => T("Bridge", "舰桥");
        public static string BridgeSubtitle(string commander) => T(
            $"Commander {commander} — live fleet & sector status",
            $"指挥官 {commander} — 舰队与星域状态");

        public static string BridgeAutoCombatBadge => T(
            "● AUTO COMBAT ONLINE",
            "● 全自动战斗已启用");

        public static string BridgeBanner => T(
            "Combat is fully automatic. Configure formation, pick a sector, then watch or skip the battle.",
            "战斗为全自动结算。配置编队、选择星域后发起战斗，可观看或跳过演出。");

        public static string StatCombatPower => T("COMBAT POWER", "战力");
        public static string StatExploration => T("EXPLORATION", "探索进度");
        public static string StatCredits => T("CREDITS", "信用点");
        public static string StatRoster => T("ROSTER", "编成");
        public static string RosterSummary(int cards, int inLine) => T(
            $"{cards} cards · {inLine} in line",
            $"{cards} 张卡 · {inLine} 上阵");

        public static string ActiveFleet => T("ACTIVE FLEET", "当前编队");
        public static string BridgeSectors => T("NEARBY SECTORS", "邻近星域");
        public static string EventLog => T("EVENT LOG", "战情日志");

        public static string EmptyFleet => T(
            "No units in formation. Open Formation to assign cards.",
            "当前没有上阵单位。请打开编队配置。");

        public static string TodaysMissions => T("MISSIONS", "任务");
        public static string BridgeOpenFormation => T("Open Formation", "打开编队");
        public static string BridgeStartAutoBattle => T("Explore / Auto Battle", "探索 / 自动战斗");
        public static string BridgeOpsHint => T(
            "Multi-deck parallel ops (gather / craft) arrive with later phases. Combat decks use 5 cards.",
            "多卡组并行行动（采集 / 制造）将在后续阶段接入。战斗卡组固定 5 人。");

        public static string SlotLabel(int index) => T($"Slot {index}", $"槽位 {index}");

        public static string SectorName(int index) => index switch
        {
            0 => T("Outer Belt", "外缘带"),
            1 => T("Mining Spur", "矿脉支线"),
            2 => T("Quantum Rift", "量子裂隙"),
            3 => T("Abyssal Edge", "深渊边界"),
            4 => T("Convoy Lane", "护航航道"),
            _ => T($"Sector {index}", $"星域 {index}")
        };

        public static string BridgeLogLine(int index, int a, int b) => index switch
        {
            0 => T($"Fleet status: {a} units deployed · roster {b}", $"舰队状态：{a} 人上阵 · 卡池 {b}"),
            1 => T($"Exploration {a}% · credits {b:N0}₵", $"探索进度 {a}% · 信用点 {b:N0}₵"),
            2 => T($"Combat power estimate {a:N0}", $"预估战力 {a:N0}"),
            3 => T("Auto-battle rules active — no manual turns required", "全自动战斗规则生效 — 无需手动回合"),
            _ => T("Ship gates & offline caps unlock with progression", "舰船门槛与离线上限随成长解锁")
        };

        // Explore
        public static string ExploreTitle => T("Explore / Auto Battle", "探索 / 自动战斗");
        public static string ExploreHint => T(
            "Select a sector to start fully automatic combat. First clear and farm both resolve without turn-by-turn input.",
            "选择星域发起全自动战斗。首次通关与挂机刷取均无需逐回合操作。");
        public static string ExploreFleetReady(int count) => count > 0
            ? T($"Formation ready: {count} / 5", $"编队就绪：{count} / 5")
            : T("Formation empty — assign cards before battle", "编队为空 — 开战前请先上阵");
        public static string ExploreRegionMeta(int index) => T(
            $"Auto combat · Ship gate TBD · Region #{index + 1}",
            $"全自动战斗 · 舰船门槛待定 · 星域 #{index + 1}");
        public static string StartAutoBattle => T("Start Auto Battle", "开始自动战斗");
        public static string ExploreSideTitle => T("AUTO COMBAT", "全自动战斗");
        public static string ExploreSideBody => T(
            "• Pre-battle: pick deck & formation\n• In-battle: system resolves turns by speed\n• Post-battle: report & rewards\n• Optional: speed up / skip presentation\n\nManual skill targeting is out of scope.",
            "• 战前：选择卡组与编队\n• 战中：系统按速度自动结算回合\n• 战后：战报与奖励\n• 可选：加速 / 跳过演出\n\n不提供手动点选技能与目标。");

        // Formation
        public static string AvailableCharacters => T("Available Characters", "可用角色");
        public static string FormationTitle => T("FORMATION", "编队配置");
        public static string FormationStats => T("Formation Stats", "编队属性");
        public static string SaveFormation => T("Save Formation", "保存编队");
        public static string AddUnit => T("+ Add", "+ 添加");
        public static string FormationHint => T(
            "Select a character on the left, then a slot.\nTap an occupied slot to remove.\nCombat decks use up to 5 cards.",
            "点击左侧角色，再点格子上阵。\n点击已占用格子可下阵。\n战斗卡组最多 5 人。");

        public static string[] FormationSlotLabels => new[]
        {
            T("Front Left", "前排左"),
            T("Front Right", "前排右"),
            T("Mid", "中排"),
            T("Back Left", "后排左"),
            T("Back Right", "后排右")
        };

        // Settings
        public static string SettingsTitle => T("Settings", "设置");
        public static string SettingsHint => T(
            "Save / clear cards / language / return to main menu.",
            "保存 / 清空卡牌 / 语言 / 返回主菜单。");
        public static string SaveCardData => T("Save Card Data", "保存卡牌数据");
        public static string ClearCardsDebug => T("Clear Cards (Debug)", "清空卡牌（调试）");
        public static string ReturnMainMenu => T("Return to Main Menu", "返回主菜单");
        public static string Language => T("Language", "语言");
        public static string LanguageEnglish => "English";
        public static string LanguageChinese => "简体中文";
        public static string CurrentPlayer(string name) => T($"Current player: {name}", $"当前玩家：{name}");

        // Missions placeholder
        public static string MissionsTitle => T("Missions", "任务");
        public static string MissionsBody => T(
            "Mission domain expands later.\nUse Explore for automatic sector combat.",
            "任务系统后续扩展。\n使用探索进行星域全自动战斗。");
        public static string EnterExploreBattle => T("Open Explore", "打开探索");

        // Battle chrome
        public static string BattleBreadcrumb => T("NEXUS › EXPLORE / AUTO BATTLE", "NEXUS › 探索 / 自动战斗");
        public static string AutoBattle => T("AUTO BATTLE", "自动战斗");
        public static string BattleEnd => T("BATTLE END", "战斗结束");
        public static string ReturnToBridge => T("Return to Bridge", "返回舰桥");
        public static string BattleStatusHint => T(
            "Fully automatic combat · Esc returns to Bridge · Report opens when finished",
            "全自动战斗 · Esc 返回舰桥 · 结束后可查看战报");
        public static string BattleLogHeader => T("BATTLE LOG", "战斗日志");
        public static string BattleLogStart => T("Auto combat started.\nResolving rounds…", "自动战斗开始。\n正在结算回合…");
        public static string BattleLogRound(int round) => T($"— Round {round} —", $"— 第 {round} 回合 —");
        public static string BattleLogEnded => T(
            "Battle ended. Open report or return to Bridge.",
            "战斗结束。可查看战报或返回舰桥。");
        public static string Turn(int round) => round > 0 ? T($"TURN  {round}", $"回合  {round}") : T("TURN  —", "回合  —");
    }
}
