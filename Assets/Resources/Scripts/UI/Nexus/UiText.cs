using Assets.Scripts.Utils;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Typed UI copy accessors. Strings live in Resources/Data/Localization/UiStrings.json.
    /// Catalog bilingual fields still use <see cref="T"/>.
    /// </summary>
    internal static class UiText
    {
        public static string T(string english, string zhCn) => LocalizationUtil.T(english, zhCn);

        private static string G(string id) => LocalizationUtil.Get(id);
        private static string F(string id, params object[] args) => LocalizationUtil.Format(id, args);

        // Shell / navigation
        public static string ScreenBridge => G("ui.nav.bridge");
        public static string ScreenBattle => G("ui.nav.explore");
        public static string ScreenFormation => G("ui.nav.formation");
        public static string ScreenCharacters => G("ui.nav.characters");
        public static string ScreenCards => G("ui.nav.cards");
        public static string ScreenInventory => G("ui.nav.inventory");
        public static string InventoryTitle => G("ui.inventory.title");
        public static string InventoryHint => G("ui.inventory.hint");
        public static string InventoryTabAll => G("ui.inventory.tab_all");
        public static string InventoryTabEquipment => G("ui.inventory.tab_equipment");
        public static string InventoryTabMaterial => G("ui.inventory.tab_material");
        public static string InventoryTabConsumable => G("ui.inventory.tab_consumable");
        public static string InventoryQualityAll => G("ui.inventory.quality_all");
        public static string InventoryEmpty => G("ui.inventory.empty");
        public static string InventoryQty(int quality, int qty) => F("ui.inventory.qty", quality, qty);
        public static string ScreenMarket => G("ui.nav.starport");
        public static string ScreenCrafting => G("ui.nav.crafting");
        public static string ScreenMissions => G("ui.nav.missions");
        public static string ScreenRecruit => G("ui.nav.recruit");
        public static string ScreenSettings => G("ui.nav.settings");
        public static string ScreenDebug => G("ui.nav.debug");

        public static string Breadcrumb(AppScreen screen) => screen switch
        {
            AppScreen.Bridge => G("ui.breadcrumb.bridge"),
            AppScreen.Battle => G("ui.breadcrumb.battle"),
            AppScreen.Formation => G("ui.breadcrumb.formation"),
            AppScreen.Ship => G("ui.breadcrumb.ship"),
            AppScreen.Characters => G("ui.breadcrumb.characters"),
            AppScreen.Cards => G("ui.breadcrumb.cards"),
            AppScreen.Inventory => G("ui.breadcrumb.inventory"),
            AppScreen.Market => G("ui.breadcrumb.market"),
            AppScreen.Recruit => G("ui.breadcrumb.recruit"),
            AppScreen.Crafting => G("ui.breadcrumb.crafting"),
            AppScreen.Missions => G("ui.breadcrumb.missions"),
            AppScreen.Settings => G("ui.breadcrumb.settings"),
            AppScreen.Debug => G("ui.breadcrumb.debug"),
            _ => screen.ToString()
        };

        public static string StatusShortcuts => G("ui.status.shortcuts");
        public static string StatusVersion => G("ui.status.version");
        public static string MainMenuTagline => G("ui.main.tagline");

        // Bridge
        public static string BridgeTitle => G("ui.bridge.title");
        public static string BridgeSubtitle(string commander) => F("ui.bridge.subtitle", commander);
        public static string BridgeAutoCombatBadge => G("ui.bridge.auto_badge");
        public static string BridgeBanner => G("ui.bridge.banner");

        public static string StatCombatPower => G("ui.stat.combat_power");
        public static string StatExploration => G("ui.stat.exploration");
        public static string StatCredits => G("ui.stat.credits");
        public static string StatRoster => G("ui.stat.roster");
        public static string RosterSummary(int cards, int inLine) => F("ui.bridge.roster_summary", cards, inLine);

        public static string ActiveFleet => G("ui.bridge.active_fleet");
        public static string RunningOps => G("ui.bridge.running_ops");
        public static string ParallelOps(int current, int max) => F("ui.bridge.parallel_ops", current, max);
        public static string BridgeSectors => G("ui.bridge.sectors");
        public static string EventLog => G("ui.bridge.event_log");

        public static string EmptyFleet => G("ui.bridge.empty_fleet");
        public static string NoRunningOps => G("ui.bridge.no_running_ops");
        public static string StopAction => G("ui.bridge.stop_action");
        public static string StopActionConfirm => G("ui.bridge.stop_confirm");

        public static string TodaysMissions => G("ui.bridge.missions");
        public static string BridgeOpenFormation => G("ui.bridge.open_formation");
        public static string BridgeStartAutoBattle => G("ui.bridge.start_auto_battle");
        public static string BridgeOpsHint => G("ui.bridge.ops_hint");

        public static string SlotLabel(int index) => F("ui.bridge.slot", index);

        public static string SectorName(int index) => index switch
        {
            0 => G("ui.sector.0"),
            1 => G("ui.sector.1"),
            2 => G("ui.sector.2"),
            3 => G("ui.sector.3"),
            4 => G("ui.sector.4"),
            _ => F("ui.sector.n", index)
        };

        public static string BridgeLogLine(int index, int a, int b) => index switch
        {
            0 => F("ui.bridge.log.0", a, b),
            1 => F("ui.bridge.log.1", a, b),
            2 => F("ui.bridge.log.2", a),
            3 => G("ui.bridge.log.3"),
            _ => G("ui.bridge.log.default")
        };

        // Explore
        public static string ExploreTitle => G("ui.explore.title");
        public static string ExploreHint => G("ui.explore.hint");
        public static string ExploreFleetReady(int count) => count > 0
            ? F("ui.explore.fleet_ready", count)
            : G("ui.explore.fleet_empty");
        public static string ExploreRegionMeta(int index) => F("ui.explore.region_meta", index + 1);
        public static string StartAutoBattle => G("ui.explore.challenge");
        public static string StartFarm => G("ui.explore.farm");
        public static string StartGather => G("ui.explore.gather");
        public static string RegionLocked => G("ui.common.locked");
        public static string FarmAvailable => G("ui.explore.farm_available");
        public static string PendingLootTitle(int count) => F("ui.explore.pending_loot_title", count);
        public static string PendingLootHint => G("ui.explore.pending_loot_hint");
        public static string PendingLootEmpty => G("ui.explore.pending_loot_empty");
        public static string ClaimPendingLoot => G("ui.explore.claim_all");
        public static string ExploreRecPower(int power) => F("ui.explore.rec_power", power);
        public static string CraftingTitle => G("ui.crafting.title");
        public static string CraftingHint => G("ui.crafting.hint");
        public static string CraftingDelivered(string name, int qty) => F("ui.crafting.delivered", qty, name);
        public static string CraftingSelectRecipe => G("ui.crafting.select_recipe");
        public static string CraftingInputs => G("ui.crafting.inputs");
        public static string CraftingOutputs => G("ui.crafting.outputs");
        public static string CraftingExpectedQuality(string range) => F("ui.crafting.expected_quality", range);
        public static string CraftingStart => G("ui.crafting.start");
        public static string CraftingMissingMats => G("ui.crafting.missing_mats");
        public static string CraftingAutoRepair => G("ui.crafting.auto_repair");
        public static string CraftingRepairFirst => G("ui.crafting.repair_first");
        public static string EquipToSelected => G("ui.crafting.equip_to_selected");
        public static string StarportShopTitle => G("ui.starport.title");
        public static string StarportShopHint => G("ui.starport.hint");
        public static string CreditsLabel(int n) => F("ui.starport.credits", n);
        public static string BoundCreditsLabel(int n) => F("ui.starport.bound", n);
        public static string PlayerMarketDisabledSolo => G("ui.starport.player_market_disabled");
        public static string OpenPlayerMarket => G("ui.starport.player_exchange");
        public static string PlayerMarketSoon => G("ui.starport.player_market_soon");
        public static string SelectShopOffer => G("ui.starport.select_offer");
        public static string OfferLocked => G("ui.common.locked");
        public static string ShopBuy => G("ui.starport.buy");
        public static string ShopSell => G("ui.starport.sell");
        public static string ShopBuyX(int n) => F("ui.starport.buy_x", n);
        public static string ShopSellX(int n) => F("ui.starport.sell_x", n);
        public static string ShopPrices(int buy, int sell) => F("ui.starport.prices", buy, sell);
        public static string SectorComplete => G("ui.explore.sector_complete");
        public static string RegionProgressLabel(string state) => state switch
        {
            "Challengeable" => G("ui.region.challengeable"),
            "Cleared" => G("ui.region.cleared"),
            "BossAvailable" => G("ui.region.boss_available"),
            "BossDefeated" => G("ui.region.boss_defeated"),
            _ => G("ui.region.locked")
        };
        public static string OpenShipBay => G("ui.ship.open");
        public static string ShipBayTitle => G("ui.ship.title");
        public static string UpgradeShipLevel(int scrap, int credit) => F("ui.ship.upgrade", scrap, credit);
        public static string CombatStrategyLabel => G("ui.ship.strategy");
        public static string CycleStrategy => G("ui.ship.cycle_strategy");
        public static string Back => G("ui.ship.back");
        public static string ExploreSideTitle => G("ui.explore.side_title");
        public static string ExploreSideBody => G("ui.explore.side_body");

        // Formation
        public static string AvailableCharacters => G("ui.formation.available");
        public static string FormationTitle => G("ui.formation.title");
        public static string FormationStats => G("ui.formation.stats");
        public static string SaveFormation => G("ui.formation.save");
        public static string SetCombatDeck => G("ui.formation.set_combat");
        public static string ActiveCombatBadge => G("ui.formation.combat_badge");
        public static string DeckLocked => G("ui.common.locked");
        public static string DeckListHeader => G("ui.formation.deck_slots");
        public static string AddUnit => G("ui.formation.add");
        public static string JoinDeck => G("ui.formation.join");
        public static string LeaveDeck => G("ui.formation.leave");
        public static string InspectHint => G("ui.formation.inspect_hint");
        public static string EquippedGear => G("ui.formation.equipped_gear");
        public static string NoGearInSlot => G("ui.formation.empty_gear");
        public static string DetailSkills => G("ui.formation.skills");
        public static string DetailExpertise => G("ui.formation.expertise");
        public static string DetailStats => G("ui.formation.combat_stats");
        public static string JoinNeedsEmptySlot => G("ui.formation.no_empty_slot");
        public static string EquipSlotLabel(string slot) => slot switch
        {
            "Weapon" => G("ui.equip.weapon"),
            "Armor" => G("ui.equip.armor"),
            "Accessory" => G("ui.equip.accessory"),
            "Tool" => G("ui.equip.tool"),
            _ => G("ui.equip.gear")
        };
        public static string FormationHint => G("ui.formation.hint");
        public static string RosterFilterAll => G("ui.formation.filter_all");
        public static string RosterEmpty => G("ui.formation.roster_empty");
        public static string RosterAssignedTo(string deckNames) => F("ui.formation.assigned_to", deckNames);
        public static string RosterAlreadyAssignedHint(string deckNames) =>
            F("ui.formation.already_assigned", deckNames);
        public static string RosterTypeChip(string value) => F("ui.formation.type_chip", value);
        public static string RosterFactionChip(string value) => F("ui.formation.faction_chip", value);
        public static string RosterRarityChip(string value) => F("ui.formation.rarity_chip", value);
        public static string RosterSortChip(string value) => F("ui.formation.sort_chip", value);
        public static string RosterSortLabel(int mode) => mode switch
        {
            1 => G("ui.sort.rarity"),
            2 => G("ui.sort.level"),
            3 => G("ui.sort.name"),
            4 => G("ui.sort.type"),
            _ => G("ui.sort.power")
        };
        public static string RosterRarityLabel(int filter) => filter switch
        {
            1 => "B+",
            2 => "A+",
            3 => "S+",
            4 => "SS",
            _ => RosterFilterAll
        };
        public static string RosterFactionLabel(int filter) => filter switch
        {
            1 => G("ui.faction.guard"),
            2 => G("ui.faction.syndicate"),
            _ => RosterFilterAll
        };
        public static string ArchetypeLabel(string archetype) => archetype switch
        {
            "Assassin" => G("ui.archetype.assassin"),
            "Magician" => G("ui.archetype.magician"),
            "Mechanician" => G("ui.archetype.mechanician"),
            "Monster" => G("ui.archetype.monster"),
            "Potioneer" => G("ui.archetype.potioneer"),
            "Warrior" => G("ui.archetype.warrior"),
            _ => G("ui.archetype.unknown")
        };
        public static string TierShort(string tierName) =>
            string.IsNullOrEmpty(tierName) || tierName == "None" ? "-" : tierName.Replace("Tier", "");
        public static string DeckBusyHint => G("ui.formation.deck_busy");
        public static string OccupationBadge(string state) => state switch
        {
            "MainCombat" => G("ui.occupation.main_combat"),
            "AutoCombat" => G("ui.occupation.auto_combat"),
            "Gathering" => G("ui.occupation.gathering"),
            "Processing" => G("ui.occupation.processing"),
            "Manufacturing" => G("ui.occupation.manufacturing"),
            "Researching" => G("ui.occupation.researching"),
            "InTransit" => G("ui.occupation.in_transit"),
            _ => ""
        };
        public static string DeckPurposeLabel(string purpose) => purpose switch
        {
            "Combat" => G("ui.purpose.combat"),
            "Gather" => G("ui.purpose.gather"),
            "Produce" => G("ui.purpose.produce"),
            "Research" => G("ui.purpose.research"),
            "Transit" => G("ui.purpose.transit"),
            _ => G("ui.purpose.flexible")
        };
        public static string DeckActionLabel(string status, string actionType) => status switch
        {
            "Running" => F("ui.deck.running", actionType),
            "PausedCap" => F("ui.deck.paused_cap", actionType),
            "Completing" => G("ui.deck.completing"),
            _ => G("ui.deck.idle")
        };
        public static string DeckUnlockHint(string en, string zh) => T(en, zh);

        public static string[] FormationSlotLabels => new[]
        {
            G("ui.formation.slot_fl"),
            G("ui.formation.slot_fr"),
            G("ui.formation.slot_mid"),
            G("ui.formation.slot_bl"),
            G("ui.formation.slot_br")
        };

        public static string DeckLineup => G("ui.formation.deck_lineup");
        public static string LevelAbbrev => G("ui.common.lv");
        public static string ListSeparator => G("ui.common.list_sep");
        public static string StatHp => G("ui.stat.hp");
        public static string StatAtk => G("ui.stat.atk");
        public static string StatDef => G("ui.stat.def");
        public static string StatAcc => G("ui.stat.acc");
        public static string StatDodge => G("ui.stat.dodge");
        public static string StatCrit => G("ui.stat.crit");
        public static string StatCritDmg => G("ui.stat.crit_dmg");
        public static string StatDr => G("ui.stat.dr");
        public static string StatEnergy => G("ui.stat.energy");
        public static string StatSpeed => G("ui.stat.speed");
        public static string None => G("ui.common.none");
        public static string SelectRosterFirst => G("ui.formation.select_roster");
        public static string NoUnequippedGear => G("ui.formation.no_unequipped");
        public static string EquippedItem(string name) => F("ui.formation.equipped", name);
        public static string EquipFailed => G("ui.formation.equip_failed");
        public static string ActionStopped => G("ui.formation.action_stopped");
        public static string SelectedDeckLabel => G("ui.formation.selected_deck");
        public static string InFormationLabel => G("ui.formation.in_formation");
        public static string TotalPowerLabel => G("ui.formation.total_power");

        // Settings
        public static string SettingsTitle => G("ui.settings.title");
        public static string SettingsHint => G("ui.settings.hint");
        public static string SaveCardData => G("ui.settings.save_cards");
        public static string ClearCardsDebug => G("ui.settings.clear_cards");
        public static string ReturnMainMenu => G("ui.settings.return_menu");
        public static string Language => G("ui.settings.language");
        public static string LanguageEnglish => G("ui.settings.lang_en");
        public static string LanguageChinese => G("ui.settings.lang_zh");
        public static string CurrentPlayer(string name) => F("ui.settings.current_player", name);
        public static string GiftCodeSection => G("ui.settings.gift_section");
        public static string GiftCodeHint => G("ui.settings.gift_hint");
        public static string GiftCodePlaceholder => G("ui.settings.gift_placeholder");
        public static string GiftCodeRedeem => G("ui.settings.gift_redeem");
        public static string DebugModeSection => G("ui.settings.debug_section");
        public static string DebugModeToggleOn => G("ui.settings.debug_on");
        public static string DebugModeToggleOff => G("ui.settings.debug_off");
        public static string DebugModeHint => G("ui.settings.debug_hint");

        // Debug screen
        public static string DebugTitle => G("ui.debug.title");
        public static string DebugHint => G("ui.debug.hint");
        public static string DebugHintAllItems => G("ui.debug.hint_items");
        public static string DebugCreditsHeader => G("ui.debug.credits_header");
        public static string DebugCreditsUpdated(int n) => F("ui.debug.credits_updated", n);
        public static string DebugItemsHeader => G("ui.debug.items_header");
        public static string DebugCardsHeader => G("ui.debug.cards_header");
        public static string DebugApplyQty => G("ui.debug.apply");
        public static string DebugInvalidNumber => G("ui.debug.invalid_qty");
        public static string DebugItemUpdated(string name, int qty) => F("ui.debug.item_updated", name, qty);
        public static string DebugItemFailed => G("ui.debug.item_failed");
        public static string DebugAddRandomCard => G("ui.debug.add_card");
        public static string DebugUpgradeFirstCard => G("ui.debug.upgrade_card");
        public static string DebugAddExpFirstCard => G("ui.debug.level_card");
        public static string DebugRefresh => G("ui.debug.refresh");
        public static string DebugNoCards => G("ui.debug.no_cards");
        public static string DebugCardAdded(string name) => F("ui.debug.card_added", name);
        public static string DebugCardFailed => G("ui.debug.card_failed");
        public static string DebugCardUpgraded(string name) => F("ui.debug.card_upgraded", name);
        public static string DebugCardLeveled(string name, int level) => F("ui.debug.card_leveled", name, level);
        public static string DebugDisable => G("ui.debug.disable");

        // Missions / onboarding
        public static string MissionsTitle => G("ui.missions.title");
        public static string MissionsBody => G("ui.missions.body");
        public static string MissionsOnboardingHint => G("ui.missions.onboarding_hint");
        public static string MissionsChainComplete => G("ui.missions.chain_complete");
        public static string MissionsStatusActive => G("ui.missions.active");
        public static string MissionsStatusDone => G("ui.missions.done");
        public static string MissionsStatusLocked => G("ui.missions.locked");
        public static string MissionsClaim => G("ui.missions.claim");
        public static string MissionsGo => G("ui.missions.go");
        public static string MissionsClaimed(string title) => F("ui.missions.claimed", title);
        public static string MissionsNextStep(string title) => F("ui.missions.next", title);
        public static string MissionsOpenMissions => G("ui.missions.open");
        public static string MissionsStepBanner(string title) => F("ui.missions.step_banner", title);
        public static string EnterExploreBattle => G("ui.missions.open_explore");

        public static string RecruitTitle => G("ui.recruit.title");
        public static string RecruitHint => G("ui.recruit.hint");
        public static string RecruitSubtitle(int tickets, int pity, int threshold) =>
            F("ui.recruit.subtitle", tickets, pity, threshold);
        public static string RecruitPullOne => G("ui.recruit.pull_one");
        public static string RecruitPullTen => G("ui.recruit.pull_ten");
        public static string RecruitBuyTickets => G("ui.recruit.buy_tickets");
        public static string RecruitNeedTickets => G("ui.recruit.need_tickets");
        public static string RecruitResultHeader(int spent, int pity) => F("ui.recruit.result", spent, pity);
        public static string BridgeOpenRecruit => G("ui.bridge.open_recruit");

        // Battle chrome
        public static string BattleBreadcrumb => G("ui.battle.breadcrumb");
        public static string AutoBattle => G("ui.battle.auto");
        public static string BattleEnd => G("ui.battle.end");
        public static string BattleVictory => G("ui.battle.victory");
        public static string BattleDefeat => G("ui.battle.defeat");
        public static string ReturnToBridge => G("ui.battle.return_bridge");
        public static string BattleStatusHint => G("ui.battle.status_hint");
        public static string BattleLogHeader => G("ui.battle.log_header");
        public static string BattleLogStart => G("ui.battle.log_start");
        public static string BattleLogRound(int round) => F("ui.battle.log_round", round);
        public static string BattleLogEnded => G("ui.battle.log_ended");
        public static string Turn(int round) => round > 0 ? F("ui.battle.turn", round) : G("ui.battle.turn_empty");
    }
}
