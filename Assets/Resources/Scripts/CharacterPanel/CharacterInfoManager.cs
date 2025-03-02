using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Resources.Scripts.Entity;
using Assets.Scripts.Utils;
using Assets.Resources.Scripts.Utils;

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
        private void Start()
        {
            // TODO:fake data

            DataUtil.SavePlayerData(playerData);
            Debug.Log("Player data has been saved:" + playerData.playerName);
            Init();
        }

        private void Init()
        {
            PlayerEntity pData = DataUtil.LoadPlayerData();
            Debug.Log("Player data has been loaded:" + pData.playerName);

            portraitButton.onClick.AddListener(() => OnPortraitImageClick());
            InitPlayerInfo();
        }

        private void InitPlayerInfo()
        {
            ImageUtil.LoadSprite(portraitImage, DataUtil.playerAvatarPath);
            playerIDText.text = "ID: " + playerData.playerName;
            playerLevelText.text = "Level: " + playerData.level.ToString();
            playerPowerText.text = "Combat Power: " + playerData.combatPower.ToString();
            playerTierText.text = "Tier: " + playerData.tier.ToString();
            playerCreditText.text = "Credit Point: " + playerData.creditPoints.ToString();
            playerTitleText.text = "Title: " + playerData.title[0];
            playerExploreText.text = "Exploration: " + playerData.explorationProgress.ToString();
        }

        private void OnPortraitImageClick()
        {
            FileBrowserHelper.OpenFileBrowser((sprite) =>
            {
                if (sprite != null)
                {
                    portraitImage.sprite = sprite;
                    ImageUtil.SaveSprite(portraitImage, DataUtil.playerAvatarPath);
                }
            });
        }
    }
}