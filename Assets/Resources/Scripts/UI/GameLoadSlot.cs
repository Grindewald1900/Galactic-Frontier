using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Planet;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Scene;

namespace Assets.Resources.Scripts.UI
{
    public class GameLoadSlot : MonoBehaviour, IPointerClickHandler
    {
        public Image portraitImage;
        public Image tierImage;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI dateText;
        public PlayerEntity playerEntity;
        public bool isFocused = false;
        private Vector3 defaultScale = Vector3.one;
        private Vector3 focusedScale = new(1.05f, 1.05f, 1f);

        public void SetPlayer(PlayerEntity player)
        {
            if (player == null) return;
            playerEntity = player;
            Debug.Log("Set player: " + player.playerName + " Level: " + player.level + " Tier: " + player.tier + " Date: " + player.saveDate);
            SetPlayerText(player.level.ToString(), player.playerName, player.saveDate);
            ImageUtil.LoadSprite(portraitImage, DataUtil.Instance.GetPlayerAvatarPath(player.playerID));
            tierImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.badgeImagePath, player.tier.ToString());
        }

        public void SetPlayerText(string level, string name, string date)
        {
            TextUtil.SetText(levelText, level);
            TextUtil.SetText(nameText, name);
            TextUtil.SetText(dateText, date);
        }

        public void SetFocus(bool focus)
        {
            isFocused = focus;
            transform.localScale = isFocused ? focusedScale : defaultScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isFocused)
            {
                GameLoadManager.Instance.SetCurrentFocus(this);
            }
            else
            {
                LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.MainScene));
            }
        }
    }
}