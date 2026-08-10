using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>
    /// Upgrades a player save directory from its on-disk version to <see cref="SaveVersion.Current"/>.
    /// </summary>
    public static class SaveMigrator
    {
        /// <summary>
        /// Reads <c>meta.json</c> (or treats missing meta as v0), rejects newer schemas, and migrates forward.
        /// </summary>
        public static SaveEnsureResult EnsureCurrent(DataUtil dataUtil, string playerSavePath)
        {
            if (dataUtil == null)
                throw new ArgumentNullException(nameof(dataUtil));
            if (string.IsNullOrWhiteSpace(playerSavePath))
                return SaveEnsureResult.Fail("Player save path is empty.");

            if (!Directory.Exists(playerSavePath))
                Directory.CreateDirectory(playerSavePath);

            var meta = dataUtil.LoadMetaFromDirectory(playerSavePath);
            var version = meta != null ? meta.saveVersion : 0;

            if (version > SaveVersion.Current)
            {
                return SaveEnsureResult.Fail(
                    $"Save version {version} is newer than this build ({SaveVersion.Current}). Please update the game.");
            }

            try
            {
                while (version < SaveVersion.Current)
                {
                    switch (version)
                    {
                        case 0:
                            if (!Migrate0To1(dataUtil, playerSavePath, meta))
                                return SaveEnsureResult.Fail("Migration 0→1 failed. Original files were preserved when possible.");
                            version = 1;
                            meta = dataUtil.LoadMetaFromDirectory(playerSavePath);
                            break;
                        case 1:
                            if (!Migrate1To2(dataUtil, playerSavePath, meta))
                                return SaveEnsureResult.Fail("Migration 1→2 failed (decks.json).");
                            version = 2;
                            meta = dataUtil.LoadMetaFromDirectory(playerSavePath);
                            break;
                        case 2:
                            if (!Migrate2To3(dataUtil, playerSavePath, meta))
                                return SaveEnsureResult.Fail("Migration 2→3 failed (world/ship).");
                            version = 3;
                            meta = dataUtil.LoadMetaFromDirectory(playerSavePath);
                            break;
                        default:
                            return SaveEnsureResult.Fail($"No migrator registered for saveVersion {version}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[SAVE] Migration failed: " + ex);
                TryQuarantine(dataUtil, playerSavePath);
                return SaveEnsureResult.Fail("Migration failed: " + ex.Message);
            }

            return SaveEnsureResult.Ok(version);
        }

        private static bool Migrate0To1(DataUtil dataUtil, string playerSavePath, SaveMeta existingMeta)
        {
            var legacyPath = Combine(playerSavePath, DefaultProperty.ITEM_DATA);
            var localPath = Combine(playerSavePath, DefaultProperty.INVENTORY_LOCAL);
            var remotePath = Combine(playerSavePath, DefaultProperty.INVENTORY_REMOTE);
            var bakPath = Combine(playerSavePath, DefaultProperty.ITEM_DATA_BAK);

            var localExists = File.Exists(localPath);
            var remoteExists = File.Exists(remotePath);
            var legacyExists = File.Exists(legacyPath);

            List<ItemEntity> localItems;
            List<ItemEntity> remoteItems;

            if (legacyExists && !(localExists && remoteExists))
            {
                var legacyItems = dataUtil.ReadItemListFile(legacyPath) ?? new List<ItemEntity>();
                localItems = legacyItems.Where(i => i != null && !i.isRemote).ToList();
                remoteItems = legacyItems.Where(i => i != null && i.isRemote).ToList();
                Debug.Log(
                    $"[SAVE] Migrating itemData.json → split inventories " +
                    $"(local={localItems.Count}, remote={remoteItems.Count}).");
            }
            else
            {
                localItems = localExists
                    ? dataUtil.ReadItemListFile(localPath) ?? new List<ItemEntity>()
                    : new List<ItemEntity>();
                remoteItems = remoteExists
                    ? dataUtil.ReadItemListFile(remotePath) ?? new List<ItemEntity>()
                    : new List<ItemEntity>();
            }

            foreach (var item in localItems)
                item.isRemote = false;
            foreach (var item in remoteItems)
                item.isRemote = true;

            if (!dataUtil.WriteItemListFile(localPath, localItems))
                return false;
            if (!dataUtil.WriteItemListFile(remotePath, remoteItems))
                return false;

            if (legacyExists)
            {
                try
                {
                    if (File.Exists(bakPath))
                        File.Delete(bakPath);
                    File.Move(legacyPath, bakPath);
                    Debug.Log($"[SAVE] Renamed legacy itemData.json → {Path.GetFileName(bakPath)}");
                }
                catch (Exception ex)
                {
                    Debug.LogError("[SAVE] Failed to rename itemData.json to .bak: " + ex.Message);
                    return false;
                }
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var meta = existingMeta ?? new SaveMeta();
            meta.saveVersion = 1;
            if (meta.createdAtUtc <= 0)
                meta.createdAtUtc = now;
            meta.lastSavedAtUtc = now;
            if (string.IsNullOrEmpty(meta.appVersion))
                meta.appVersion = Application.version;

            return dataUtil.WriteMetaToDirectory(playerSavePath, meta);
        }

        /// <summary>Introduces decks.json from legacy CardEntity.LineupPosition (P1.1).</summary>
        private static bool Migrate1To2(DataUtil dataUtil, string playerSavePath, SaveMeta existingMeta)
        {
            var decksPath = Combine(playerSavePath, DefaultProperty.DECKS_DATA);
            if (!File.Exists(decksPath))
            {
                var cards = dataUtil.ReadCardListFromDirectory(playerSavePath) ?? new List<CardEntity>();
                var lineup = new List<LegacyLineupEntry>();
                foreach (var card in cards)
                {
                    if (card == null) continue;
                    var pos = card.GetLineupPosition();
                    if (pos == LineupPosition.None) continue;
                    lineup.Add(new LegacyLineupEntry(card.id, (int)pos));
                }

                var state = DeckStateFactory.CreateFromLegacyLineup(lineup);
                if (!dataUtil.WriteDeckStateToDirectory(playerSavePath, state))
                    return false;
                Debug.Log($"[SAVE] Migration 1→2 wrote decks.json (combat members={state.decks[0].MemberCount}).");
            }
            else
            {
                Debug.Log("[SAVE] Migration 1→2: decks.json already present.");
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var meta = existingMeta ?? new SaveMeta();
            meta.saveVersion = SaveVersion.Decks;
            if (meta.createdAtUtc <= 0)
                meta.createdAtUtc = now;
            meta.lastSavedAtUtc = now;
            if (string.IsNullOrEmpty(meta.appVersion))
                meta.appVersion = Application.version;

            return dataUtil.WriteMetaToDirectory(playerSavePath, meta);
        }

        /// <summary>Introduces world.json + ship.json (P2).</summary>
        private static bool Migrate2To3(DataUtil dataUtil, string playerSavePath, SaveMeta existingMeta)
        {
            var worldPath = Combine(playerSavePath, DefaultProperty.WORLD_DATA);
            if (!File.Exists(worldPath))
            {
                var world = WorldRules.CreateNewPlayerWorld();
                if (!dataUtil.WriteWorldStateToDirectory(playerSavePath, world))
                    return false;
                Debug.Log("[SAVE] Migration 2→3 wrote world.json.");
            }

            var shipPath = Combine(playerSavePath, DefaultProperty.SHIP_DATA);
            if (!File.Exists(shipPath))
            {
                var ship = ShipRules.CreateStarterShip();
                if (!dataUtil.WriteShipStateToDirectory(playerSavePath, ship))
                    return false;
                Debug.Log("[SAVE] Migration 2→3 wrote ship.json.");
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var meta = existingMeta ?? new SaveMeta();
            meta.saveVersion = SaveVersion.WorldAndShip;
            if (meta.createdAtUtc <= 0)
                meta.createdAtUtc = now;
            meta.lastSavedAtUtc = now;
            if (string.IsNullOrEmpty(meta.appVersion))
                meta.appVersion = Application.version;

            return dataUtil.WriteMetaToDirectory(playerSavePath, meta);
        }

        private static void TryQuarantine(DataUtil dataUtil, string playerSavePath)
        {
            try
            {
                var root = Directory.GetParent(playerSavePath)?.FullName;
                if (string.IsNullOrEmpty(root))
                    return;

                var corruptRoot = Path.Combine(root, "_corrupt");
                dataUtil.CheckIfPathExist(corruptRoot);
                var folderName = Path.GetFileName(playerSavePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                var stamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var dest = Path.Combine(corruptRoot, $"{folderName}_{stamp}");
                // Copy, do not move — keep original for the player.
                CopyDirectory(playerSavePath, dest);
                Debug.LogWarning($"[SAVE] Copied save to quarantine: {dest}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SAVE] Quarantine copy failed: " + ex.Message);
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(destDir, name), overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var name = Path.GetFileName(dir);
                CopyDirectory(dir, Path.Combine(destDir, name));
            }
        }

        private static string Combine(string directory, string fileName)
        {
            var relative = fileName.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(directory, relative);
        }
    }
}
