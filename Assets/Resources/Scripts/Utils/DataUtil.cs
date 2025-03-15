using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.CharacterPanel;

namespace Assets.Resources.Scripts.Utils
{
    public class DataUtil : MonoBehaviour
    {
        public static DataUtil Instance { get; private set; }
        public PlayerEntity currentPlayer;
        private List<PlayerEntity> playerEntities = new();
        private List<ExpertiseEntity> expertiseEntities = new();

        // e.g C:/Users/.../Galactic Frontier/saves
        private string savePath;
        // e.g C:/Users/.../Galactic Frontier/saves/9ea194d8-ab32-47c0-a023-4c0c040c4c0a
        private string playerSavePath;
        // e.g C:/Users/.../Galactic Frontier/saves/9ea194d8-ab32-47c0-a023-4c0c040c4c0a/playerData.json
        private string playerDataPath;
        // e.g C:/Users/.../Galactic Frontier/saves/9ea194d8-ab32-47c0-a023-4c0c040c4c0a/playerCards.json
        private string playerCardPath;
        // e.g C:/Users/.../Galactic Frontier/saves/9ea194d8-ab32-47c0-a023-4c0c040c4c0a/itemData.json
        private string itemDataPath;
        // e.g C:/Users/.../Galactic Frontier/saves/9ea194d8-ab32-47c0-a023-4c0c040c4c0a/avatar.png
        public string playerAvatarPath;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 场景切换时不销毁
            }
            else
            {
                Destroy(gameObject); // 如果已有实例，销毁新创建的对象
            }
        }

        void Start()
        {
            InitPaths();
        }

        public void InitPlayerInfo()
        {
            if (!File.Exists(savePath + DefaultProperty.PLAYER_ENTITIES))
            {
                File.WriteAllText(savePath + DefaultProperty.PLAYER_ENTITIES, "");
            }
        }

        private void InitPaths()
        {
            savePath = Application.persistentDataPath + "/saves";
            CheckIfPathExist(savePath);
        }

        public void UpdatePaths()
        {
            playerSavePath = GetPlayerSavePath(currentPlayer.playerID);
            playerDataPath = GetPlayerDataPath(currentPlayer.playerID);
            playerCardPath = GetPlayerCardPath(currentPlayer.playerID);
            itemDataPath = GetPlayerItemDataPath(currentPlayer.playerID);
            playerAvatarPath = GetPlayerAvatarPath(currentPlayer.playerID);

            Debug.Log("数据路径已初始化：");
            Debug.Log("savePath: " + savePath);
            Debug.Log("playerSavePath: " + playerSavePath);
            Debug.Log("playerDataPath: " + playerDataPath);
            Debug.Log("playerCardPath: " + playerCardPath);
            Debug.Log("itemDataPath: " + itemDataPath);
            Debug.Log("playerAvatarPath: " + playerAvatarPath);
        }

        public bool CreatePlayerData()
        {
            currentPlayer = new PlayerEntity();
            UpdatePaths();
            return SavePlayerData(currentPlayer);
        }

        public void SaveItemData(List<ItemEntity> items)
        {
            SaveData(new ItemListWrapper { items = items, count = items.Count }, playerSavePath, DefaultProperty.ITEM_DATA);
        }

        public void SaveCardData(List<CardEntity> cards)
        {
            SaveData(new CardListWrapper { cardEntities = cards, count = cards.Count }, playerSavePath, DefaultProperty.PLAYER_CARDS_DATA);
        }

        public void SaveExpertiseData(List<ExpertiseEntity> expertiseEntities)
        {
            SaveData(new ExpertiseListWrapper { expertiseEntities = expertiseEntities, count = expertiseEntities.Count }, playerSavePath, DefaultProperty.EXPERT_DATA);
        }

        // Save player data in its individual directory e.g. saves/asdhjakh123uh2insajdia/playerData.json
        public bool SavePlayerData(PlayerEntity playerEntity)
        {
            SaveData(playerEntity, playerSavePath, DefaultProperty.PLAYER_DATA);
            LoadPlayerEntities();
            return true;
        }

        // Save playerEntities in root directory e.g, e.g. saves/playerEntities.json
        public void SavePlayerEntities()
        {
            SaveData(new PlayerListWrapper { playerEntities = playerEntities, count = playerEntities.Count }, savePath, DefaultProperty.PLAYER_ENTITIES);
        }

        public void SaveGameData()
        {
            SavePlayerData(CharacterInfoManager.Instance.playerData);
            SaveCardData(CardListManager.Instance.cardEntities);
        }

        public List<ItemEntity> LoadItemData()
        {
            if (File.Exists(itemDataPath))
            {
                string json = File.ReadAllText(itemDataPath);
                string encryptedJson = DecryptBase64(json);
                ItemListWrapper wrapper = JsonUtility.FromJson<ItemListWrapper>(encryptedJson);
                Debug.Log("物品数据已加载：" + itemDataPath + "，共" + wrapper.items.Count + "个物品");
                return wrapper.items;
            }
            else
            {
                Debug.LogWarning("未找到存档文件，返回默认物品列表");
                return new List<ItemEntity>();
            }
        }

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
            else
            {
                Debug.LogWarning("未找到存档，返回默认卡片列表");
                return new List<CardEntity>();
            }
        }

        public List<ExpertiseEntity> LoadExpertiseData()
        {
            if (File.Exists(playerSavePath + DefaultProperty.EXPERT_DATA))
            {
                string json = File.ReadAllText(playerSavePath + DefaultProperty.EXPERT_DATA);
                string encryptedJson = DecryptBase64(json);
                ExpertiseListWrapper wrapper = JsonUtility.FromJson<ExpertiseListWrapper>(encryptedJson);
                Debug.Log("专长数据已加载：" + playerSavePath + DefaultProperty.EXPERT_DATA + "，共" + wrapper.expertiseEntities.Count + "个专长");
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
            else
            {
                Debug.LogWarning("No player data found, returning default player data");
            }
            return currentPlayer;
        }

        public List<PlayerEntity> LoadPlayerEntities()
        {
            playerEntities.Clear();

            // Load all player data from individual directory e.g. saves/asdhjakh123uh2insajdia/playerData.json
            foreach (string id in GetAllPlayerIDs())
            {
                string path = savePath + "/" + id + DefaultProperty.PLAYER_DATA;
                playerEntities.Add(LoadPlayerData(path));
            }
            Debug.Log("已加载存档数据：共" + playerEntities.Count + "个存档");
            return playerEntities;
        }

        // Get all player IDs from file name in saves directory 
        public List<string> GetAllPlayerIDs()
        {
            List<string> ids = new();
            foreach (string p in Directory.GetDirectories(savePath))
            {
                Debug.Log("存在的存档：" + Path.GetFileName(p));
                ids.Add(Path.GetFileName(p));
            }
            return ids;
        }

        public PlayerEntity GetCurrentPlayer()
        {
            return currentPlayer;
        }

        public void SetCurrentPlayer(PlayerEntity player)
        {
            currentPlayer = player;
            UpdatePaths();
        }

        public string GetPlayerSavePath(string playerID)
        {
            return savePath + "/" + playerID;
        }

        public string GetPlayerDataPath(string playerID)
        {
            return savePath + "/" + playerID + DefaultProperty.PLAYER_DATA;
        }

        public string GetPlayerCardPath(string playerID)
        {
            return savePath + "/" + playerID + DefaultProperty.PLAYER_CARDS_DATA;
        }

        public string GetPlayerItemDataPath(string playerID)
        {
            return savePath + "/" + playerID + DefaultProperty.ITEM_DATA;
        }

        public string GetPlayerAvatarPath(string playerID)
        {
            return savePath + "/" + playerID + DefaultProperty.AVATER;
        }

        public string EncryptBase64(string plainText)
        {
            if (DefaultProperty.isDebug)
            {
                return plainText;
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(bytes);
            }
        }

        public string DecryptBase64(string encryptedText)
        {
            if (DefaultProperty.isDebug)
            {
                return encryptedText;
            }
            else
            {
                byte[] bytes = Convert.FromBase64String(encryptedText);
                return Encoding.UTF8.GetString(bytes);
            }
        }

        public void SaveData(object data, string filePath, string fileName)
        {
            CheckIfPathExist(filePath);

            string json = JsonUtility.ToJson(data, true);
            string outputJson = DefaultProperty.isDebug ? json : EncryptBase64(json); ;
            File.WriteAllText(filePath + fileName, outputJson);
            Debug.Log("SaveData:" + fileName + " saved at " + filePath);
        }

        public void CheckIfPathExist(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) // **如果文件夹不存在**
            {
                Directory.CreateDirectory(directoryPath); // **创建文件夹**
                Debug.Log($"Path created: {directoryPath}");
            }
        }

        [Serializable]
        public class ItemListWrapper
        {
            public int count = 0;
            public List<ItemEntity> items;
        }
    }
}