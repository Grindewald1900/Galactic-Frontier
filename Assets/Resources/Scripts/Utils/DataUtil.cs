using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;

namespace Assets.Resources.Scripts.Utils
{
    /// <summary>
    /// Owns the active player save context and serializes player, card, expertise, and item data.
    /// This component survives scene changes; callers must select <see cref="currentPlayer"/>
    /// before using any player-scoped load or save method.
    /// </summary>
    /// <remarks>
    /// Save files live under Application.persistentDataPath/saves/{playerId}. Base64 encoding is
    /// an optional storage format controlled by DefaultProperty.isDebug, not a security boundary.
    /// Writes use a temp file then replace for crash safety (P0.2).
    /// </remarks>
    public class DataUtil : MonoBehaviour
    {
        private static readonly HashSet<string> ReservedSaveDirectoryNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "_dev", "_corrupt" };

        public static DataUtil Instance { get; private set; }

        /// <summary>The player whose directory is currently bound to the cached save paths.</summary>
        public PlayerEntity currentPlayer;

        /// <summary>Last migration / save readiness error for UI messaging.</summary>
        public string LastSaveError { get; private set; }

        private List<PlayerEntity> playerEntities = new();
        private List<ExpertiseEntity> expertiseEntities = new();

        private string savePath;
        private string playerSavePath;
        private string playerDataPath;
        private string playerCardPath;
        private string inventoryLocalPath;
        private string inventoryRemotePath;
        private string metaPath;
        public string playerAvatarPath;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitPaths();
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitPlayerInfo()
        {
            string playerEntitiesPath = CombinePath(savePath, DefaultProperty.PLAYER_ENTITIES);
            if (!File.Exists(playerEntitiesPath))
            {
                File.WriteAllText(playerEntitiesPath, string.Empty);
            }
        }

        private void InitPaths()
        {
            savePath = Path.Combine(Application.persistentDataPath, "saves");
            CheckIfPathExist(savePath);
        }

        /// <summary>
        /// Rebinds all cached player-scoped paths after creating or selecting a player.
        /// </summary>
        public void UpdatePaths()
        {
            if (currentPlayer == null)
                throw new InvalidOperationException("A current player must be selected before updating save paths.");
            if (string.IsNullOrWhiteSpace(currentPlayer.playerID))
                throw new InvalidOperationException("The current player must have a valid ID.");

            playerSavePath = GetPlayerSavePath(currentPlayer.playerID);
            playerDataPath = GetPlayerDataPath(currentPlayer.playerID);
            playerCardPath = GetPlayerCardPath(currentPlayer.playerID);
            inventoryLocalPath = GetInventoryPath(currentPlayer.playerID, InventoryStore.Local);
            inventoryRemotePath = GetInventoryPath(currentPlayer.playerID, InventoryStore.Remote);
            metaPath = CombinePath(playerSavePath, DefaultProperty.META_DATA);
            playerAvatarPath = GetPlayerAvatarPath(currentPlayer.playerID);

            Debug.Log("数据路径已初始化：");
            Debug.Log("savePath: " + savePath);
            Debug.Log("playerSavePath: " + playerSavePath);
            Debug.Log("playerDataPath: " + playerDataPath);
            Debug.Log("playerCardPath: " + playerCardPath);
            Debug.Log("inventoryLocalPath: " + inventoryLocalPath);
            Debug.Log("inventoryRemotePath: " + inventoryRemotePath);
            Debug.Log("metaPath: " + metaPath);
            Debug.Log("playerAvatarPath: " + playerAvatarPath);
        }

        /// <summary>Creates a new player identity, binds its save directory, writes seed + decks + meta.</summary>
        public bool CreatePlayerData()
        {
            LastSaveError = null;
            currentPlayer = new PlayerEntity();
            UpdatePaths();
            CheckIfPathExist(playerSavePath);

            // Write profile without touching meta (meta is created below).
            if (!SaveData(currentPlayer, playerSavePath, DefaultProperty.PLAYER_DATA))
            {
                LastSaveError = "Failed to write playerData.json.";
                return false;
            }

            LoadPlayerEntities();

            var seedVersion = StarterSeedApplier.ApplyNewPlayerLocalInventory(this);
            if (!SaveInventory(InventoryStore.Remote, new List<ItemEntity>(), touchMeta: false))
            {
                LastSaveError = "Failed to write inventory_remote.json.";
                return false;
            }

            DeckService.CreateForNewPlayer(this);
            WorldService.CreateForNewPlayer(this);
            ShipService.CreateForNewPlayer(this);
            Assets.Resources.Scripts.Economy.IdleSettlementService.CreateForNewPlayer(this);

            // Starter credits so Outer Belt ship upgrades are reachable.
            if (currentPlayer.creditPoints < 50)
                currentPlayer.creditPoints = 50;
            SaveData(currentPlayer, playerSavePath, DefaultProperty.PLAYER_DATA);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var meta = new SaveMeta
            {
                saveVersion = SaveVersion.Current,
                createdAtUtc = now,
                lastSavedAtUtc = now,
                starterSeedTableVersion = seedVersion,
                appVersion = Application.version
            };

            if (!SaveMeta(meta))
            {
                LastSaveError = "Failed to write meta.json.";
                return false;
            }

            return true;
        }

        /// <summary>Saves one inventory store file and optionally refreshes meta timestamps.</summary>
        public bool SaveInventory(InventoryStore store, List<ItemEntity> items, bool touchMeta = true)
        {
            EnsurePlayerBound();
            items ??= new List<ItemEntity>();
            var isRemote = store == InventoryStore.Remote;
            foreach (var item in items)
            {
                if (item != null)
                    item.isRemote = isRemote;
            }

            var fileName = store == InventoryStore.Local
                ? DefaultProperty.INVENTORY_LOCAL
                : DefaultProperty.INVENTORY_REMOTE;
            var ok = SaveData(
                new ItemListWrapper { items = items, count = items.Count },
                playerSavePath,
                fileName);
            if (ok && touchMeta)
                TouchMetaLastSaved();
            return ok;
        }

        /// <summary>Loads one inventory store; missing file yields an empty list.</summary>
        public List<ItemEntity> LoadInventory(InventoryStore store)
        {
            EnsurePlayerBound();
            var path = store == InventoryStore.Local ? inventoryLocalPath : inventoryRemotePath;
            var items = ReadItemListFile(path) ?? new List<ItemEntity>();
            var isRemote = store == InventoryStore.Remote;
            foreach (var item in items)
            {
                if (item != null)
                    item.isRemote = isRemote;
            }

            Debug.Log($"物品数据已加载：{path}，共{items.Count}个物品 ({store})");
            return items;
        }

        [Obsolete("Use SaveInventory(InventoryStore, List<ItemEntity>). Legacy shared itemData.json is migrator-only.")]
        public void SaveItemData(List<ItemEntity> items)
        {
            Debug.LogWarning("[SAVE] SaveItemData is obsolete; use SaveInventory.");
            SaveInventory(InventoryStore.Local, items);
        }

        [Obsolete("Use LoadInventory(InventoryStore). Legacy shared itemData.json is migrator-only.")]
        public List<ItemEntity> LoadItemData()
        {
            Debug.LogWarning("[SAVE] LoadItemData is obsolete; use LoadInventory.");
            return LoadInventory(InventoryStore.Local);
        }

        public void SaveCardData(List<CardEntity> cards)
        {
            SaveData(new CardListWrapper { cardEntities = cards, count = cards.Count }, playerSavePath, DefaultProperty.PLAYER_CARDS_DATA);
            TouchMetaLastSaved();
        }

        public void SaveExpertiseData(List<ExpertiseEntity> expertiseEntities)
        {
            SaveData(new ExpertiseListWrapper { expertiseEntities = expertiseEntities, count = expertiseEntities.Count }, playerSavePath, DefaultProperty.EXPERT_DATA);
            TouchMetaLastSaved();
        }

        public bool SavePlayerData(PlayerEntity playerEntity)
        {
            if (playerEntity == null || string.IsNullOrWhiteSpace(playerEntity.playerID))
            {
                LastSaveError = "Refused to write a player profile without an ID.";
                Debug.LogError("[SAVE] " + LastSaveError);
                return false;
            }

            var ok = SaveData(playerEntity, playerSavePath, DefaultProperty.PLAYER_DATA);
            LoadPlayerEntities();
            if (ok)
                TouchMetaLastSaved();
            return ok;
        }

        public void SavePlayerEntities()
        {
            SaveData(new PlayerListWrapper { playerEntities = playerEntities, count = playerEntities.Count }, savePath, DefaultProperty.PLAYER_ENTITIES);
        }

        /// <summary>
        /// Saves the player profile, cards, domain state, and both inventories, then refreshes meta.
        /// </summary>
        /// <remarks>
        /// Empty in-memory collections are skipped rather than written. A manager whose scene has been
        /// unloaded still answers through its static instance but reports no contents, and flushing that
        /// would blank a healthy file. Every subsystem already persists on mutation, so the emptied case
        /// is on disk by the time this runs and nothing is lost by skipping it.
        /// </remarks>
        public void SaveGameData()
        {
            SavePlayerData(currentPlayer);

            var cards = CardListManager.Instance?.cardEntities;
            if (cards != null && cards.Count > 0)
                SaveCardData(cards);

            DeckService.Save(this);
            WorldService.Save(this);
            ShipService.Save(this);

            SaveInventoryIfPopulated(InventoryStore.Local, ItemManager.Instance?.GetItems());
            SaveInventoryIfPopulated(InventoryStore.Remote, RemoteItemManager.Instance?.GetItems());

            TouchMetaLastSaved();
        }

        private void SaveInventoryIfPopulated(InventoryStore store, List<ItemEntity> items)
        {
            if (items != null && items.Count > 0)
                SaveInventory(store, items, touchMeta: false);
        }

        /// <summary>
        /// Autosave entry point for lifecycle hooks (quit, backgrounding, leaving for the main menu).
        /// Never throws: a save failure must not block the shutdown or scene change that triggered it.
        /// </summary>
        public bool TrySaveGameData()
        {
            if (currentPlayer == null ||
                string.IsNullOrWhiteSpace(currentPlayer.playerID) ||
                string.IsNullOrWhiteSpace(playerSavePath))
                return false;

            try
            {
                SaveGameData();
                return true;
            }
            catch (Exception ex)
            {
                LastSaveError = ex.Message;
                Debug.LogError($"[SAVE] Autosave failed: {ex}");
                return false;
            }
        }

        private void OnApplicationQuit()
        {
            TrySaveGameData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                TrySaveGameData();
        }

        public bool SaveDeckState(PlayerDeckState state, bool touchMeta = true)
        {
            EnsurePlayerBound();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.decks?.Count ?? 0;
            var ok = SaveData(state, playerSavePath, DefaultProperty.DECKS_DATA);
            if (ok && touchMeta)
                TouchMetaLastSaved();
            return ok;
        }

        public PlayerDeckState LoadDeckState()
        {
            EnsurePlayerBound();
            return ReadDeckStateFromDirectory(playerSavePath);
        }

        public PlayerDeckState ReadDeckStateFromDirectory(string directory)
        {
            var path = CombinePath(directory, DefaultProperty.DECKS_DATA);
            if (!File.Exists(path))
                return null;
            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                return JsonUtility.FromJson<PlayerDeckState>(decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read decks.json: " + ex.Message);
                return null;
            }
        }

        public bool WriteDeckStateToDirectory(string directory, PlayerDeckState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.decks?.Count ?? 0;
            return SaveData(state, directory, DefaultProperty.DECKS_DATA);
        }

        public bool SaveWorldState(PlayerWorldState state, bool touchMeta = true)
        {
            EnsurePlayerBound();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.regions?.Count ?? 0;
            var ok = SaveData(state, playerSavePath, DefaultProperty.WORLD_DATA);
            if (ok && touchMeta)
                TouchMetaLastSaved();
            return ok;
        }

        public PlayerWorldState LoadWorldState()
        {
            EnsurePlayerBound();
            return ReadWorldStateFromDirectory(playerSavePath);
        }

        public PlayerWorldState ReadWorldStateFromDirectory(string directory)
        {
            var path = CombinePath(directory, DefaultProperty.WORLD_DATA);
            if (!File.Exists(path))
                return null;
            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                return JsonUtility.FromJson<PlayerWorldState>(decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read world.json: " + ex.Message);
                return null;
            }
        }

        public bool WriteWorldStateToDirectory(string directory, PlayerWorldState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.regions?.Count ?? 0;
            return SaveData(state, directory, DefaultProperty.WORLD_DATA);
        }

        public bool SaveShipState(ShipEntity state, bool touchMeta = true)
        {
            EnsurePlayerBound();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.modules?.Count ?? 0;
            var ok = SaveData(state, playerSavePath, DefaultProperty.SHIP_DATA);
            if (ok && touchMeta)
                TouchMetaLastSaved();
            return ok;
        }

        public ShipEntity LoadShipState()
        {
            EnsurePlayerBound();
            return ReadShipStateFromDirectory(playerSavePath);
        }

        public ShipEntity ReadShipStateFromDirectory(string directory)
        {
            var path = CombinePath(directory, DefaultProperty.SHIP_DATA);
            if (!File.Exists(path))
                return null;
            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                return JsonUtility.FromJson<ShipEntity>(decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read ship.json: " + ex.Message);
                return null;
            }
        }

        public bool WriteShipStateToDirectory(string directory, ShipEntity state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.modules?.Count ?? 0;
            return SaveData(state, directory, DefaultProperty.SHIP_DATA);
        }

        public bool SaveIdleState(Assets.Resources.Scripts.Economy.Domain.PlayerIdleState state, bool touchMeta = true)
        {
            EnsurePlayerBound();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.pendingLoot?.Count ?? 0;
            var ok = SaveData(state, playerSavePath, DefaultProperty.IDLE_DATA);
            if (ok && touchMeta)
                TouchMetaLastSaved();
            return ok;
        }

        public Assets.Resources.Scripts.Economy.Domain.PlayerIdleState LoadIdleState()
        {
            EnsurePlayerBound();
            return ReadIdleStateFromDirectory(playerSavePath);
        }

        public Assets.Resources.Scripts.Economy.Domain.PlayerIdleState ReadIdleStateFromDirectory(string directory)
        {
            var path = CombinePath(directory, DefaultProperty.IDLE_DATA);
            if (!File.Exists(path))
                return null;
            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                return JsonUtility.FromJson<Assets.Resources.Scripts.Economy.Domain.PlayerIdleState>(decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read idle.json: " + ex.Message);
                return null;
            }
        }

        public bool WriteIdleStateToDirectory(string directory, Assets.Resources.Scripts.Economy.Domain.PlayerIdleState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.pendingLoot?.Count ?? 0;
            return SaveData(state, directory, DefaultProperty.IDLE_DATA);
        }

        public bool SaveOnboardingState(
            Assets.Resources.Scripts.Onboarding.Domain.OnboardingState state, bool touchMeta = true)
        {
            EnsurePlayerBound();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.completedStepIds?.Count ?? 0;
            var ok = SaveData(state, playerSavePath, DefaultProperty.ONBOARDING_DATA);
            if (ok && touchMeta)
                TouchMetaLastSaved();
            return ok;
        }

        public Assets.Resources.Scripts.Onboarding.Domain.OnboardingState LoadOnboardingState()
        {
            EnsurePlayerBound();
            return ReadOnboardingStateFromDirectory(playerSavePath);
        }

        public Assets.Resources.Scripts.Onboarding.Domain.OnboardingState ReadOnboardingStateFromDirectory(
            string directory)
        {
            var path = CombinePath(directory, DefaultProperty.ONBOARDING_DATA);
            if (!File.Exists(path))
                return null;
            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                return JsonUtility.FromJson<Assets.Resources.Scripts.Onboarding.Domain.OnboardingState>(decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read onboarding.json: " + ex.Message);
                return null;
            }
        }

        public bool WriteOnboardingStateToDirectory(
            string directory, Assets.Resources.Scripts.Onboarding.Domain.OnboardingState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.completedStepIds?.Count ?? 0;
            return SaveData(state, directory, DefaultProperty.ONBOARDING_DATA);
        }

        public bool SaveFeatureUnlockState(
            Assets.Resources.Scripts.Unlock.Domain.FeatureUnlockState state, bool touchMeta = true)
        {
            EnsurePlayerBound();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.unlockedFeatureIds?.Count ?? 0;
            var ok = SaveData(state, playerSavePath, DefaultProperty.UNLOCKS_DATA);
            if (ok && touchMeta)
                TouchMetaLastSaved();
            return ok;
        }

        public Assets.Resources.Scripts.Unlock.Domain.FeatureUnlockState LoadFeatureUnlockState()
        {
            EnsurePlayerBound();
            return ReadFeatureUnlockStateFromDirectory(playerSavePath);
        }

        public Assets.Resources.Scripts.Unlock.Domain.FeatureUnlockState ReadFeatureUnlockStateFromDirectory(
            string directory)
        {
            var path = CombinePath(directory, DefaultProperty.UNLOCKS_DATA);
            if (!File.Exists(path))
                return null;
            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                return JsonUtility.FromJson<Assets.Resources.Scripts.Unlock.Domain.FeatureUnlockState>(decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read unlocks.json: " + ex.Message);
                return null;
            }
        }

        public bool WriteFeatureUnlockStateToDirectory(
            string directory, Assets.Resources.Scripts.Unlock.Domain.FeatureUnlockState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.unlockedFeatureIds?.Count ?? 0;
            return SaveData(state, directory, DefaultProperty.UNLOCKS_DATA);
        }

        public bool SaveGachaState(
            Assets.Resources.Scripts.Gacha.Domain.PlayerGachaState state, bool touchMeta = true)
        {
            EnsurePlayerBound();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.totalPulls;
            var ok = SaveData(state, playerSavePath, DefaultProperty.GACHA_DATA);
            if (ok && touchMeta)
                TouchMetaLastSaved();
            return ok;
        }

        public Assets.Resources.Scripts.Gacha.Domain.PlayerGachaState LoadGachaState()
        {
            EnsurePlayerBound();
            return ReadGachaStateFromDirectory(playerSavePath);
        }

        public Assets.Resources.Scripts.Gacha.Domain.PlayerGachaState ReadGachaStateFromDirectory(string directory)
        {
            var path = CombinePath(directory, DefaultProperty.GACHA_DATA);
            if (!File.Exists(path))
                return null;
            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                return JsonUtility.FromJson<Assets.Resources.Scripts.Gacha.Domain.PlayerGachaState>(decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read gacha.json: " + ex.Message);
                return null;
            }
        }

        public bool WriteGachaStateToDirectory(
            string directory, Assets.Resources.Scripts.Gacha.Domain.PlayerGachaState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            state.count = state.totalPulls;
            return SaveData(state, directory, DefaultProperty.GACHA_DATA);
        }

        public List<CardEntity> ReadCardListFromDirectory(string directory)
        {
            var path = CombinePath(directory, DefaultProperty.PLAYER_CARDS_DATA);
            if (!File.Exists(path))
                return new List<CardEntity>();
            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                var wrapper = JsonUtility.FromJson<CardListWrapper>(decoded);
                return wrapper?.cardEntities ?? new List<CardEntity>();
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read playerCards.json during migration: " + ex.Message);
                return new List<CardEntity>();
            }
        }

        /// <summary>Loads the active player's persisted card collection, or an empty list when absent.</summary>
        public List<CardEntity> LoadCardData()
        {
            if (File.Exists(playerCardPath))
            {
                string json = File.ReadAllText(playerCardPath);
                string encryptedJson = DecryptBase64(json);
                CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(encryptedJson);
                Debug.Log("卡片数据已加载：" + playerCardPath + "，共" + wrapper.cardEntities.Count + "张卡片");
                return wrapper.cardEntities;
            }

            Debug.LogWarning("未找到存档，返回默认卡片列表");
            return new List<CardEntity>();
        }

        public List<ExpertiseEntity> LoadExpertiseData()
        {
            string expertiseDataPath = CombinePath(playerSavePath, DefaultProperty.EXPERT_DATA);
            if (File.Exists(expertiseDataPath))
            {
                string json = File.ReadAllText(expertiseDataPath);
                string encryptedJson = DecryptBase64(json);
                ExpertiseListWrapper wrapper = JsonUtility.FromJson<ExpertiseListWrapper>(encryptedJson);
                Debug.Log($"专长数据已加载：{expertiseDataPath}，共{wrapper.expertiseEntities.Count}个专长");
                expertiseEntities = wrapper.expertiseEntities;
            }
            else
            {
                Debug.LogWarning("未找到存档，返回默认专长列表");
            }
            return expertiseEntities;
        }

        public PlayerEntity LoadPlayerData(string dataPath)
        {
            if (File.Exists(dataPath))
            {
                string json = File.ReadAllText(dataPath);
                string encryptedJson = DecryptBase64(json);
                return JsonUtility.FromJson<PlayerEntity>(encryptedJson);
            }

            Debug.LogWarning("No player data found, returning default player data");
            return currentPlayer;
        }

        public List<PlayerEntity> LoadPlayerEntities()
        {
            playerEntities.Clear();

            foreach (string id in GetAllPlayerIDs())
            {
                PlayerEntity player = ReadPlayerProfile(GetPlayerDataPath(id));
                if (player == null)
                {
                    Debug.LogWarning($"[SAVE] Skipping save '{id}': playerData.json is missing or unreadable.");
                    continue;
                }

                // The directory name is the authoritative id, so a profile written without one is
                // recoverable rather than fatal to the load screen.
                if (string.IsNullOrWhiteSpace(player.playerID))
                {
                    Debug.LogWarning($"[SAVE] Save '{id}' has no player ID; recovering it from the directory name.");
                    player.playerID = id;
                    if (string.IsNullOrWhiteSpace(player.playerName))
                        player.playerName = "Player";
                    SaveData(player, GetPlayerSavePath(id), DefaultProperty.PLAYER_DATA);
                }

                playerEntities.Add(player);
            }
            Debug.Log("已加载存档数据：共" + playerEntities.Count + "个存档");
            return playerEntities;
        }

        /// <summary>Reads one profile file, returning null when it is absent or cannot be parsed.</summary>
        private PlayerEntity ReadPlayerProfile(string dataPath)
        {
            if (!File.Exists(dataPath))
                return null;

            try
            {
                return JsonUtility.FromJson<PlayerEntity>(DecryptBase64(File.ReadAllText(dataPath)));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SAVE] Failed to parse {dataPath}: {ex.Message}");
                return null;
            }
        }

        public List<string> GetAllPlayerIDs()
        {
            List<string> ids = new();
            if (!Directory.Exists(savePath))
                return ids;

            foreach (string p in Directory.GetDirectories(savePath))
            {
                var name = Path.GetFileName(p);
                if (ReservedSaveDirectoryNames.Contains(name))
                    continue;
                Debug.Log("存在的存档：" + name);
                ids.Add(name);
            }
            return ids;
        }

        public PlayerEntity GetCurrentPlayer()
        {
            return currentPlayer;
        }

        /// <summary>
        /// Selects the active player, updates paths, and migrates the directory to the current schema.
        /// </summary>
        /// <returns>False when the save is newer than this build or migration fails.</returns>
        public bool SetCurrentPlayer(PlayerEntity player)
        {
            currentPlayer = player;
            UpdatePaths();
            return EnsurePlayerSaveReady();
        }

        /// <summary>Runs migrators so the bound player directory matches <see cref="SaveVersion.Current"/>.</summary>
        public bool EnsurePlayerSaveReady()
        {
            LastSaveError = null;
            EnsurePlayerBound();
            var result = SaveMigrator.EnsureCurrent(this, playerSavePath);
            if (!result.Success)
            {
                LastSaveError = result.Message;
                Debug.LogError("[SAVE] " + result.Message);
                return false;
            }

            return true;
        }

        public string GetPlayerSavePath(string playerID)
        {
            ValidatePlayerId(playerID);
            return Path.Combine(savePath, playerID);
        }

        public string GetPlayerDataPath(string playerID)
        {
            return CombinePath(GetPlayerSavePath(playerID), DefaultProperty.PLAYER_DATA);
        }

        public string GetPlayerCardPath(string playerID)
        {
            return CombinePath(GetPlayerSavePath(playerID), DefaultProperty.PLAYER_CARDS_DATA);
        }

        public string GetInventoryPath(string playerID, InventoryStore store)
        {
            var file = store == InventoryStore.Local
                ? DefaultProperty.INVENTORY_LOCAL
                : DefaultProperty.INVENTORY_REMOTE;
            return CombinePath(GetPlayerSavePath(playerID), file);
        }

        [Obsolete("Legacy shared item file. Use GetInventoryPath.")]
        public string GetPlayerItemDataPath(string playerID)
        {
            return CombinePath(GetPlayerSavePath(playerID), DefaultProperty.ITEM_DATA);
        }

        public string GetPlayerAvatarPath(string playerID)
        {
            return CombinePath(GetPlayerSavePath(playerID), DefaultProperty.AVATAR);
        }

        public SaveMeta LoadMeta()
        {
            EnsurePlayerBound();
            return LoadMetaFromDirectory(playerSavePath);
        }

        public SaveMeta LoadMetaFromDirectory(string directory)
        {
            var path = CombinePath(directory, DefaultProperty.META_DATA);
            if (!File.Exists(path))
                return null;

            try
            {
                var json = File.ReadAllText(path);
                var decoded = DecryptBase64(json);
                return JsonUtility.FromJson<SaveMeta>(decoded);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Failed to read meta.json: " + ex.Message);
                return null;
            }
        }

        public bool SaveMeta(SaveMeta meta)
        {
            EnsurePlayerBound();
            return WriteMetaToDirectory(playerSavePath, meta);
        }

        public bool WriteMetaToDirectory(string directory, SaveMeta meta)
        {
            if (meta == null)
                throw new ArgumentNullException(nameof(meta));
            return SaveData(meta, directory, DefaultProperty.META_DATA);
        }

        public void TouchMetaLastSaved()
        {
            if (currentPlayer == null || string.IsNullOrEmpty(playerSavePath))
                return;
            if (!File.Exists(metaPath) && !File.Exists(CombinePath(playerSavePath, DefaultProperty.META_DATA)))
                return;

            var meta = LoadMetaFromDirectory(playerSavePath) ?? new SaveMeta
            {
                saveVersion = SaveVersion.Current,
                createdAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                appVersion = Application.version
            };
            meta.saveVersion = SaveVersion.Current;
            meta.lastSavedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (string.IsNullOrEmpty(meta.appVersion))
                meta.appVersion = Application.version;
            WriteMetaToDirectory(playerSavePath, meta);
        }

        /// <summary>Reads an item list file at an absolute path (used by migrator and inventory load).</summary>
        public List<ItemEntity> ReadItemListFile(string absolutePath)
        {
            if (!File.Exists(absolutePath))
                return null;

            try
            {
                var json = File.ReadAllText(absolutePath);
                var decoded = DecryptBase64(json);
                var wrapper = JsonUtility.FromJson<ItemListWrapper>(decoded);
                return wrapper?.items ?? new List<ItemEntity>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SAVE] Failed to read item list at {absolutePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>Atomically writes an item list to an absolute path.</summary>
        public bool WriteItemListFile(string absolutePath, List<ItemEntity> items)
        {
            items ??= new List<ItemEntity>();
            var directory = Path.GetDirectoryName(absolutePath);
            var fileName = Path.GetFileName(absolutePath);
            return SaveData(
                new ItemListWrapper { items = items, count = items.Count },
                directory,
                fileName);
        }

        public string EncryptBase64(string plainText)
        {
            if (DefaultProperty.isDebug)
            {
                return plainText;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        public string DecryptBase64(string encryptedText)
        {
            if (DefaultProperty.isDebug)
            {
                return encryptedText;
            }

            byte[] bytes = Convert.FromBase64String(encryptedText);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Serializes an object with JsonUtility and atomically writes it into the save directory.
        /// </summary>
        public bool SaveData(object data, string filePath, string fileName)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A save directory is required.", nameof(filePath));

            CheckIfPathExist(filePath);

            try
            {
                string json = JsonUtility.ToJson(data, true);
                string outputJson = EncryptBase64(json);
                string relativeFileName = fileName.TrimStart(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                string outputPath = Path.Combine(filePath, relativeFileName);
                string tempPath = Path.Combine(filePath, "." + relativeFileName + ".tmp");

                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                File.WriteAllText(tempPath, outputJson);

                if (!AtomicReplace(tempPath, outputPath))
                {
                    LastSaveError = $"Failed to replace {relativeFileName}.";
                    return false;
                }

                Debug.Log($"Saved {relativeFileName} at {outputPath}.");
                return true;
            }
            catch (Exception ex)
            {
                LastSaveError = ex.Message;
                Debug.LogError($"[SAVE] Failed to save {fileName}: {ex.Message}");
                return false;
            }
        }

        private static bool AtomicReplace(string tempPath, string targetPath)
        {
            try
            {
                if (File.Exists(targetPath))
                {
                    var backupPath = targetPath + ".replacebak";
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Replace(tempPath, targetPath, backupPath);
                    try
                    {
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                    }
                    catch
                    {
                        // Non-fatal: replace already succeeded.
                    }
                }
                else
                {
                    File.Move(tempPath, targetPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SAVE] Atomic replace failed for {targetPath}: {ex.Message}");
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // ignore cleanup errors
                }

                return false;
            }
        }

        public void CheckIfPathExist(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("A directory path is required.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"Path created: {directoryPath}");
            }
        }

        private void EnsurePlayerBound()
        {
            if (currentPlayer == null || string.IsNullOrWhiteSpace(playerSavePath))
                throw new InvalidOperationException("A current player must be selected before save I/O.");
        }

        private static string CombinePath(string directory, string fileName)
        {
            string relativeFileName = fileName.TrimStart(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            return Path.Combine(directory, relativeFileName);
        }

        private static void ValidatePlayerId(string playerID)
        {
            if (string.IsNullOrWhiteSpace(playerID))
                throw new ArgumentException("A player ID is required.", nameof(playerID));
            if (playerID.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("The player ID contains invalid path characters.", nameof(playerID));
            if (ReservedSaveDirectoryNames.Contains(playerID))
                throw new ArgumentException("The player ID is reserved.", nameof(playerID));
        }

        [Serializable]
        public class ItemListWrapper
        {
            public int count = 0;
            public List<ItemEntity> items;
        }
    }
}
