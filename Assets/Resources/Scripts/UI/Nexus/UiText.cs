using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.World.Domain;
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

        /// <summary>Localized item display name from <see cref="ItemDef"/>.</summary>
        public static string ItemName(ItemDef def) =>
            def == null ? "" : T(def.displayNameEn, def.displayNameZh);

        /// <summary>Localized item lore / usage description from <see cref="ItemDef"/>.</summary>
        public static string ItemDescription(ItemDef def)
        {
            if (def == null) return "";
            var en = def.descriptionEn ?? "";
            var zh = string.IsNullOrEmpty(def.descriptionZh) ? en : def.descriptionZh;
            return T(en, zh);
        }

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
        public static string InventoryQtyOnly(int qty) => F("ui.inventory.qty_only", qty);
        public static string InventoryHintHover => G("ui.inventory.hint_hover");

        public static string ItemCategoryLabel(ItemCategory category) => category switch
        {
            ItemCategory.Material => G("ui.item.cat_material"),
            ItemCategory.Intermediate => G("ui.item.cat_intermediate"),
            ItemCategory.Consumable => G("ui.item.cat_consumable"),
            ItemCategory.Equipment => G("ui.item.cat_equipment"),
            ItemCategory.ShipModule => G("ui.item.cat_ship_module"),
            _ => G("ui.item.cat_material")
        };

        public static string ItemQualityLabel(int quality) => F("ui.item.quality", quality);
        public static string ItemAcquireHeading => G("ui.item.acquire_heading");
        public static string ItemAcquireNone => G("ui.item.acquire_none");
        public static string ItemAcquireLocked => G("ui.item.acquire_locked");
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
        public static string LoadDelete => G("ui.load.delete");
        public static string LoadDeleteConfirm => G("ui.load.delete_confirm");
        public static string LoadDeleteConfirmTitle => G("ui.load.delete_confirm_title");
        public static string LoadDeleteConfirmBody(string playerName) => F("ui.load.delete_confirm_body", playerName);

        // Reward popup
        public static string RewardTitle => G("ui.reward.title");
        public static string RewardHint => G("ui.reward.hint");
        public static string RewardEmpty => G("ui.reward.empty");
        public static string RewardConfirm => G("ui.reward.confirm");
        public static string RewardDismissHint => G("ui.reward.dismiss_hint");
        public static string GotIt => G("ui.common.got_it");
        public static string Later => G("ui.common.later");
        public static string NotificationIdle => G("ui.notify.idle");
        public static string NotificationPendingLoot(int n) => F("ui.notify.pending_loot", n);

        public static string FeatureLockedTitle => G("ui.unlock.locked_title");
        public static string FeatureLockedBody(string name) => F("ui.unlock.locked_body", name);
        public static string FeatureUnlockedTitle(string name) => F("ui.unlock.unlocked_title", name);

        public static string FleetHeading => G("ui.bridge.fleet_heading");
        public static string FleetDeckLabel(string name) => F("ui.bridge.fleet_deck", name);
        public static string FleetStatusIdle => G("ui.bridge.fleet_idle");
        public static string FleetBerthLocked => G("ui.bridge.fleet_berth");
        public static string FleetBerthHint => G("ui.bridge.fleet_berth_hint");

        public static string ProfileSection => G("ui.settings.profile");
        public static string ProfileNameLabel => G("ui.settings.profile_name");
        public static string ProfileNamePlaceholder => G("ui.settings.profile_name_ph");
        public static string ProfileSaveName => G("ui.settings.profile_save_name");
        public static string ProfileIdLabel(string id) => F("ui.settings.profile_id", id);
        public static string ProfileIdHint => G("ui.settings.profile_id_hint");
        public static string ProfileAvatarLabel => G("ui.settings.profile_avatar");
        public static string ProfileAvatarUpload => G("ui.settings.profile_avatar_upload");
        public static string ProfileAvatarEmpty => G("ui.settings.profile_avatar_empty");
        public static string ProfileAvatarInvalidType => G("ui.settings.profile_avatar_invalid_type");
        public static string ProfileAvatarTooLarge => G("ui.settings.profile_avatar_too_large");
        public static string ProfileAvatarBadImage => G("ui.settings.profile_avatar_bad_image");
        public static string ProfileAvatarSaveFailed => G("ui.settings.profile_avatar_save_failed");
        public static string ProfileSaved => G("ui.settings.profile_saved");
        public static string ProfileNameInvalid => G("ui.settings.profile_name_invalid");
        public static string ProfileAvatarSaved => G("ui.settings.profile_avatar_saved");
        public static string ProfileFrameLabel => G("ui.settings.profile_frame");
        public static string ProfileFrameEquipped => G("ui.settings.profile_frame_equipped");
        public static string ProfileFrameLockedTitle => G("ui.settings.profile_frame_locked_title");
        public static string ProfileFrameLockedBody(string name, string hint) =>
            F("ui.settings.profile_frame_locked_body", name, hint);
        public static string ProfileFrameUnlockedTitle(string name) => F("ui.frame.unlocked_title", name);
        public static string ProfileFrameUnlockedBody(string hint) => F("ui.frame.unlocked_body", hint);
        public static string ProfileFrameUnlockedNotify(string name) => F("ui.frame.unlocked_notify", name);
        public static string GameSaved => G("ui.settings.game_saved");

        // Branding
        public static string BrandTitle => G("ui.brand.title");
        /// <summary>The game name in the other language, shown under the title on the main menu.</summary>
        public static string BrandTitleAlt => G("ui.brand.title_alt");
        public static string Loading => G("ui.common.loading");

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
        public static string EditFormationHint => G("ui.bridge.edit_formation_hint");
        public static string RunningOps => G("ui.bridge.running_ops");
        public static string ParallelOps(int current, int max) => F("ui.bridge.parallel_ops", current, max);
        public static string BridgeSectors => G("ui.bridge.sectors");
        public static string EventLog => G("ui.bridge.event_log");
        public static string EventLogFilterAll => G("ui.bridge.log_filter_all");
        public static string EventLogEmpty => G("ui.bridge.log_empty");
        public static string EventLogFilter(BridgeLogCategory category) => category switch
        {
            BridgeLogCategory.Explore => G("ui.bridge.log_filter_explore"),
            BridgeLogCategory.Combat => G("ui.bridge.log_filter_combat"),
            BridgeLogCategory.Production => G("ui.bridge.log_filter_prod"),
            BridgeLogCategory.Trade => G("ui.bridge.log_filter_trade"),
            _ => G("ui.bridge.log_filter_system")
        };

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
        public static string GatherBankTitle => G("ui.explore.gather_bank_title");
        public static string GatherBankHint => G("ui.explore.gather_bank_hint");
        public static string GatherBankEmpty => G("ui.explore.gather_bank_empty");
        public static string GatherBankFull => G("ui.explore.gather_bank_full");
        public static string CollectGather(int total) => F("ui.explore.collect_gather", total);
        public static string RegionLocked => G("ui.common.locked");
        public static string FarmAvailable => G("ui.explore.farm_available");
        public static string PendingLootTitle(int count) => F("ui.explore.pending_loot_title", count);
        public static string PendingLootHint => G("ui.explore.pending_loot_hint");
        public static string PendingLootEmpty => G("ui.explore.pending_loot_empty");
        public static string ClaimPendingLoot => G("ui.explore.claim_all");
        public static string ExploreRecPower(int power) => F("ui.explore.rec_power", power);
        public static string ExploreMapLabel => G("ui.explore.map_label");
        public static string ExploreUniverseLabel => G("ui.explore.universe_label");
        public static string ExploreZoomOut => G("ui.explore.zoom_out");
        public static string ExploreZoomIn => G("ui.explore.zoom_in");
        public static string ExploreZoomScale(float zoom, bool universe) => universe
            ? F("ui.explore.zoom_universe", (int)System.Math.Round(zoom * 100f))
            : F("ui.explore.zoom_sector", (int)System.Math.Round(zoom * 100f));
        public static string ExploreEnterSector => G("ui.explore.enter_sector");
        public static string ExploreSectorSealed => G("ui.explore.sector_sealed");
        public static string ExploreSectorListTitle(int count) => F("ui.explore.sector_list_title", count);
        public static string ExploreSectorListHint => G("ui.explore.sector_list_hint");
        public static string ExploreShipMarker => G("ui.explore.ship_marker");
        public static string ExploreCruiseRandom => G("ui.explore.cruise_random");
        public static string ExploreSailHere => G("ui.explore.sail_here");
        public static string ExploreDocked => G("ui.explore.docked");
        public static string ExploreArrived => G("ui.explore.arrived");
        public static string ExploreCruiseFailed => G("ui.explore.cruise_failed");
        public static string ExploreNavStatus(float x, float y, float radar) =>
            F("ui.explore.nav_status", x.ToString("0"), y.ToString("0"), radar.ToString("0"));
        public static string ExploreGridProgress(int percent) => F("ui.explore.grid_progress", percent);
        public static string ExploreSailEta(float dist, float etaSec) =>
            F("ui.explore.sail_eta", dist.ToString("0.0"), (int)System.Math.Ceiling(etaSec));
        public static string ExploreChartedTitle(int count) => F("ui.explore.charted_title", count);
        public static string ExploreChartedHint => G("ui.explore.charted_hint");
        public static string ExploreChartedEmpty => G("ui.explore.charted_empty");
        public static string ExploreListDocked => G("ui.explore.list_docked");
        public static string ExploreListDist(float dist) => F("ui.explore.list_dist", dist.ToString("0.0"));
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
        public static string ScreenShip => G("ui.nav.ship");
        public static string ShipAppearance => G("ui.ship.appearance");
        public static string ShipHullClass(string name, int level) => F("ui.ship.hull_class", name, level);
        public static string ShipAppearanceHint => G("ui.ship.appearance_hint");
        public static string ShipModules => G("ui.ship.modules");
        public static string ShipModuleUpgrade => G("ui.ship.module_upgrade");
        public static string ShipModuleLevel(int level) => F("ui.ship.module_level", level);
        public static string ShipModuleUpgradeTitle(string name) => F("ui.ship.module_upgrade_title", name);
        public static string ShipModuleLevelNext(int current, int next) => F("ui.ship.module_level_next", current, next);
        public static string ShipModuleUpgradeCosts => G("ui.ship.module_upgrade_costs");
        public static string ShipModuleCreditCost(int have, int need) => F("ui.ship.module_credit_cost", have, need);
        public static string ShipModuleConfirmUpgrade => G("ui.ship.module_confirm");
        public static string ShipModuleCannotAfford => G("ui.ship.module_cannot_afford");
        public static string ShipSummary(int level, int credits) => F("ui.ship.summary", level, credits);
        public static string ShipStatRange => G("ui.ship.stat_range");
        public static string ShipStatEnergy => G("ui.ship.stat_energy");
        public static string ShipStatHull => G("ui.ship.stat_hull");
        public static string ShipStatEntropy => G("ui.ship.stat_entropy");
        public static string ShipStatCargo => G("ui.ship.stat_cargo");
        public static string ShipStatScan => G("ui.ship.stat_scan");
        public static string ShipStatLife => G("ui.ship.stat_life");
        public static string ShipMascot => G("ui.ship.mascot");
        public static string ShipMascotHint => G("ui.ship.mascot_hint");
        public static string ShipMascotEmpty => G("ui.ship.mascot_empty");
        public static string ShipMascotChoose => G("ui.ship.mascot_choose");
        public static string ShipMascotChange => G("ui.ship.mascot_change");
        public static string ShipMascotClear => G("ui.ship.mascot_clear");
        public static string ShipMascotPicker => G("ui.ship.mascot_picker");
        public static string ShipMascotNoneOwned => G("ui.ship.mascot_none_owned");
        public static string ExploreSideTitle => G("ui.explore.side_title");
        public static string ExploreSideBody => G("ui.explore.side_body");
        public static string ExploreProbe => G("ui.explore.probe");
        public static string ExploreStabilize => G("ui.explore.stabilize");
        public static string ExploreBuyChart(int credits) => F("ui.explore.buy_chart", credits);
        public static string ExploreFogName => G("ui.explore.fog_name");
        public static string ExploreSoftGate(int rec, int extraPct) => F("ui.explore.soft_gate", rec, extraPct);
        public static string ExploreCruiseCost(float mult, bool autoReturn) => autoReturn
            ? F("ui.explore.cruise_mult", mult.ToString("0.00"))
            : F("ui.explore.cruise_mult_no_return", mult.ToString("0.00"));
        public static string GridStateLabel(GridNodeState state) => state switch
        {
            GridNodeState.Fogged => G("ui.explore.grid_fogged"),
            GridNodeState.Located => G("ui.explore.grid_located"),
            GridNodeState.Provisional => G("ui.explore.grid_provisional"),
            GridNodeState.Stable => G("ui.explore.grid_stable"),
            _ => G("ui.explore.grid_unobserved")
        };
        public static string GridRouteLabel(GridRouteTag tag) => tag switch
        {
            GridRouteTag.Military => G("ui.explore.route_military"),
            GridRouteTag.Industry => G("ui.explore.route_industry"),
            GridRouteTag.Trade => G("ui.explore.route_trade"),
            GridRouteTag.Rift => G("ui.explore.route_rift"),
            _ => ""
        };

        // Formation
        public static string AvailableCharacters => G("ui.formation.available");
        public static string FormationTitle => G("ui.formation.title");
        public static string FormationStats => G("ui.formation.stats");
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
        public static string SnackbarFleetBusy => G("ui.snackbar.fleet_busy");
        public static string SnackbarParallelLimit => G("ui.snackbar.parallel_limit");
        public static string SnackbarCardOccupied => G("ui.snackbar.card_occupied");
        public static string SnackbarEmptyDeck => G("ui.snackbar.empty_deck");
        public static string SnackbarRegionUnavailable => G("ui.snackbar.region_unavailable");
        public static string SnackbarFarmLocked => G("ui.snackbar.farm_locked");

        public static string DeckCommandMessage(DeckCommandResult result)
        {
            if (result == null) return "";
            return result.Error switch
            {
                DeckCommandError.DeckBusy => SnackbarFleetBusy,
                DeckCommandError.ParallelLimit => SnackbarParallelLimit,
                DeckCommandError.CardOccupied => SnackbarCardOccupied,
                DeckCommandError.EmptyDeck => SnackbarEmptyDeck,
                DeckCommandError.DeckLocked => DeckBusyHint,
                _ => string.IsNullOrEmpty(result.Message) ? SnackbarFleetBusy : result.Message
            };
        }

        public static string OccupationBadge(string state) => state switch
        {
            "MainCombat" => G("ui.occupation.main_combat"),
            "AutoCombat" => G("ui.occupation.auto_combat"),
            "Idle" => G("ui.occupation.idle"),
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
        public static string Close => G("ui.common.close");
        public static string CreditsName => G("ui.common.credits");
        public static string CharactersTitle => G("ui.characters.title");
        public static string CharactersHint(int unique, int copies) => F("ui.characters.hint", unique, copies);
        public static string CharacterCopies(int count) => F("ui.characters.copies", count);
        public static string CharacterCopyList => G("ui.characters.copy_list");
        public static string CardsTitle => G("ui.cards.title");
        public static string CardsHint(int count) => F("ui.cards.hint", count);
        public static string CardFilterAll => G("ui.cards.filter_all");
        public static string CardFilterIdle => G("ui.cards.filter_idle");
        public static string CardFilterBound => G("ui.cards.filter_bound");
        public static string CardSelectHint => G("ui.cards.select_hint");
        public static string CardBound => G("ui.cards.bound");
        public static string OccupationIdle => G("ui.occupation.idle");
        public static string Dismantle => G("ui.dismantle.action");
        public static string DismantleConfirm => G("ui.dismantle.confirm");
        public static string DismantleConfirmTitle => G("ui.dismantle.confirm_title");
        public static string DismantleConfirmBody(string name, int credits) => F("ui.dismantle.confirm_body", name, credits);
        public static string DismantleRewardTitle => G("ui.dismantle.reward_title");
        public static string DismantleBlockedBound => G("ui.dismantle.blocked_bound");
        public static string DismantleBlockedBusy => G("ui.dismantle.blocked_busy");
        public static string DismantleBlockedSlotted => G("ui.dismantle.blocked_slotted");
        public static string DismantleBlockedMascot => G("ui.dismantle.blocked_mascot");
        public static string DismantleBlockedMissing => G("ui.dismantle.blocked_missing");
        public static string SelectRosterFirst => G("ui.formation.select_roster");
        public static string EquippedItem(string name) => F("ui.formation.equipped", name);
        public static string EquipFailed => G("ui.formation.equip_failed");
        public static string AutoEquip => G("ui.formation.auto_equip");
        public static string AutoEquipDone(int count) => F("ui.formation.auto_equip_done", count);
        public static string AutoEquipNone => G("ui.formation.auto_equip_none");
        public static string GearDetail => G("ui.formation.gear_detail");
        public static string GearReplacements => G("ui.formation.gear_replace");
        public static string GearNoneAvailable => G("ui.formation.gear_none_available");
        public static string UnequipGear => G("ui.formation.unequip");
        public static string UnequippedItem(string name) => F("ui.formation.unequipped", name);
        public static string GearScore(int score) => F("ui.formation.gear_score", score);
        public static string GearDurability(int current, int max) => F("ui.formation.gear_durability", current, max);
        public static string GearQuality(int quality) => F("ui.formation.gear_quality", quality);
        public static string GearEquippedBadge => G("ui.formation.gear_equipped_badge");
        public static string DeckPromotedToCombat => G("ui.formation.deck_promoted");
        public static string DeckSwapBusy => G("ui.formation.deck_swap_busy");
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

        public static string DebugMapGenHeader => G("ui.debug.map_gen_header");
        public static string DebugMapGenRegenerate => G("ui.debug.map_gen_regenerate");
        public static string DebugMapGenValidate => G("ui.debug.map_gen_validate");
        public static string DebugMapGenNoData => G("ui.debug.map_gen_no_data");
        public static string DebugMapGenValid => G("ui.debug.map_gen_valid");
        public static string DebugMapGenInvalid => G("ui.debug.map_gen_invalid");
        public static string DebugMapGenSummary(int sectors, int routes, int seed) =>
            F("ui.debug.map_gen_summary", sectors, routes, seed);
        public static string DebugMapGenPlane(string primary, string secondary, string gap) =>
            F("ui.debug.map_gen_plane", primary, secondary, gap);

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

        // P6 Commander loop
        public static string NavGroupBridge => G("ui.nav.group.bridge");
        public static string NavGroupStarMap => G("ui.nav.group.starmap");
        public static string NavGroupFleet => G("ui.nav.group.fleet");
        public static string NavGroupIndustry => G("ui.nav.group.industry");
        public static string NavGroupStarport => G("ui.nav.group.starport");
        public static string StatusBarSummary(int lv, int credits, int running, int berths, int queue, int cargo, string cruise) =>
            F("ui.status.bar_summary", lv, credits, running, berths, queue, cargo, cruise);
        public static string StatusCruiseActive => G("ui.status.cruise_active");
        public static string StatusCruiseIdle => G("ui.status.cruise_idle");
        public static string QuestTrackerTitle => G("ui.quest.tracker_title");
        public static string QuestNextGoalTitle => G("ui.quest.next_goal_title");
        public static string QuestNextGoalSteps => G("ui.quest.next_goal_steps");
        public static string QuestStepActive => G("ui.quest.step_active");
        public static string QuestStepDone => G("ui.quest.step_done");
        public static string QuestStepLocked => G("ui.quest.step_locked");
        public static string QuestClaim => G("ui.quest.claim");
        public static string QuestGo => G("ui.quest.go");
        public static string QuestToggle => G("ui.quest.toggle");
        public static string QuestChapterTitle => G("ui.quest.chapter_title");
        public static string QuestOnboardingTitle => G("ui.quest.onboarding_title");
        public static string CommanderGoalChapter => G("ui.commander.goal_chapter");
        public static string ChapterStrategyTitle => G("ui.chapter.strategy_title");
        public static string ChapterStrategyBody => G("ui.chapter.strategy_body");
        public static string ChapterRecruitHint => G("ui.chapter.recruit_hint");
        public static string ChapterScanHint => G("ui.chapter.scan_hint");
        public static string ShortageItemLabel(string name, int qty) => F("ui.shortage.item", name, qty);
        public static string ShortageCreditsLabel(int amount) => F("ui.shortage.credits", amount);
        public static string ShortageGoAcquire => G("ui.shortage.go_acquire");
        public static string ShortageGoMarket => G("ui.shortage.go_market");
        public static string BridgeCommanderGoal => G("ui.bridge.commander_goal");
        public static string BridgeBottleneck => G("ui.bridge.bottleneck");
        public static string BridgeRecommended => G("ui.bridge.recommended");
        public static string BridgeMiniMap => G("ui.bridge.mini_map");
        public static string CommanderGoalOnboarding => G("ui.commander.goal_onboarding");
        public static string CommanderGoalPostOnboarding => G("ui.commander.goal_post");
        public static string CommanderGoalScanHint => G("ui.commander.scan_hint");
        public static string MenuSettings => G("ui.menu.settings");
        public static string MenuDebug => G("ui.menu.debug");
        public static string ExploreLegend => G("ui.explore.legend");
        public static string ExploreStatusDiscovery(string s) => F("ui.explore.status_discovery", s);
        public static string ExploreStatusLane(string s) => F("ui.explore.status_lane", s);
        public static string ExploreStatusDev(string s) => F("ui.explore.status_dev", s);
        public static string ExploreStatusFleet(string s) => F("ui.explore.status_fleet", s);
        public static string FormationFrontRow => G("ui.formation.front_row");
        public static string FormationBackRow => G("ui.formation.back_row");
        public static string FormationTeamSummary => G("ui.formation.team_summary");
        public static string BattleNextAction => G("ui.battle.next_action");
        public static string CraftQueueTitle => G("ui.craft.queue_title");
        public static string CraftRecipesTitle => G("ui.craft.recipes_title");
        public static string CraftOutputTitle => G("ui.craft.output_title");
        public static string ShipExpeditionBottleneck => G("ui.ship.expedition_bottleneck");
        public static string MarketNeedFilter => G("ui.market.need_filter");
        public static string MarketItemUse(string use) => F("ui.market.item_use", use);
        public static string RecruitPoolTitle => G("ui.recruit.pool_title");
        public static string RecruitPity(int current, int max) => F("ui.recruit.pity", current, max);
        public static string RecruitTicketSources => G("ui.recruit.ticket_sources");
        public static string InventoryNeedFilter => G("ui.inventory.need_filter");
        public static string InventorySource(string s) => F("ui.inventory.source", s);
        public static string InventoryUse(string s) => F("ui.inventory.use", s);
        public static string CharactersProfileHint => G("ui.characters.profile_hint");
        public static string CardsManageHint => G("ui.cards.manage_hint");
        public static string AccessibilityContrastNote => G("ui.accessibility.contrast_note");

        public static string DialogueBack => G("ui.dialogue.back");
        public static string DialogueForward => G("ui.dialogue.forward");
        public static string DialogueFastForward => G("ui.dialogue.fast_forward");
        public static string DialogueSkip => G("ui.dialogue.skip");
        public static string DialogueClose => G("ui.dialogue.close");
        public static string DialogueLog => G("ui.dialogue.log");
        public static string DialogueLogTitle => G("ui.dialogue.log_title");
        public static string DialoguePage(int current, int total) => F("ui.dialogue.page", current, total);
        public static string DebugTestDialogue => G("ui.debug.test_dialogue");

        public static string NavGroupLabel(NavGroup group) => group switch
        {
            NavGroup.Bridge => NavGroupBridge,
            NavGroup.StarMap => NavGroupStarMap,
            NavGroup.Fleet => NavGroupFleet,
            NavGroup.Industry => NavGroupIndustry,
            NavGroup.Starport => NavGroupStarport,
            _ => NavGroupBridge
        };
    }
}
