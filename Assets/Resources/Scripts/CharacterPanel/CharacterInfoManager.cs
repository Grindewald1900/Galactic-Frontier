using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Resources.Scripts.Entity;
using Assets.Scripts.Utils;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Props;

namespace Assets.Resources.Scripts.CharacterPanel
{
    public class CharacterInfoManager : MonoBehaviour
    {
        public static CharacterInfoManager Instance;
        public Button portraitButton;
        public Image portraitImage;
        public PlayerEntity playerData;
        public TextMeshProUGUI playerIDText;
        public TextMeshProUGUI playerLevelText;
        public TextMeshProUGUI playerPowerText;
        public TextMeshProUGUI playerTierText;
        public TextMeshProUGUI playerCreditText;
        public TextMeshProUGUI playerTitleText;
        public TextMeshProUGUI playerExploreText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            playerData = DataUtil.Instance.currentPlayer;
            if (playerData == null)
            {
                Debug.LogError("playerData instance is null.");
            }
            Init();
        }

        private void Init()
        {
            portraitButton.onClick.AddListener(() => OnPortraitImageClick());
            InitPlayerInfo();
        }

        private void InitPlayerInfo()
        {
            ImageUtil.LoadSprite(portraitImage, DataUtil.Instance.playerAvatarPath);
            playerIDText.text = "ID: " + playerData.playerName;
            playerLevelText.text = "Level: " + playerData.level.ToString();
            playerPowerText.text = "Combat Power: " + playerData.combatPower.ToString();
            playerTierText.text = "Tier: " + playerData.tier.ToString();
            playerCreditText.text = "Credit Point: " + playerData.creditPoints.ToString();
            playerExploreText.text = "Exploration: " + playerData.explorationProgress.ToString();
            InitTitle();
        }

        private void InitTitle()
        {
            if (playerData.selectedTitle.Length > 0)
            {
                playerTitleText.text = "Title: " + playerData.selectedTitle;
            }
            else
            {
                if (playerData.titles?.Count > 0)
                {
                    playerTitleText.text = "Title: " + playerData.titles[0];
                }
                else
                {
                    playerTitleText.text = "Title: None";
                }
            }
        }
        private void OnPortraitImageClick()
        {
            FileBrowserHelper.OpenFileBrowser((sprite) =>
            {
                if (sprite != null)
                {
                    portraitImage.sprite = sprite;
                    ImageUtil.SaveSprite(portraitImage, DataUtil.Instance.playerAvatarPath);
                    PlayerPrefs.SetString(DefaultProperty.AVATAR_PATH, DataUtil.Instance.playerAvatarPath); // **记录路径**
                    PlayerPrefs.Save();
                }
            });
        }
    }
}