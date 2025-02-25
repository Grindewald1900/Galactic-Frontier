using TMPro;
using UnityEngine;

public class CharacterInfoManager : MonoBehaviour
{
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
        playerData = new PlayerEntity("Faker", "00001", 19, 1080, CharacterTier.TierA, 1000, new string[] { "Newbie" }, 0, new string[] { "Basic Attack" });
        DataUtil.SavePlayerData(playerData);
        Debug.Log("Player data has been saved:" + playerData.playerName);
        PlayerEntity pData = DataUtil.LoadPlayerData();
        Debug.Log("Player data has been loaded:" + pData.playerName);
        InitPlayerInfo();
    }

    private void InitPlayerInfo()
    {
        playerIDText.text = "ID: " + playerData.playerName;
        playerLevelText.text = "Level: " + playerData.level.ToString();
        playerPowerText.text = "Combat Power: " + playerData.combatPower.ToString();
        playerTierText.text = "Tier: " + playerData.tier.ToString();
        playerCreditText.text = "Credit Point: " + playerData.creditPoints.ToString();
        playerTitleText.text = "Title: " + playerData.title[0];
        playerExploreText.text = "Exploration: " + playerData.explorationProgress.ToString();
    }
}