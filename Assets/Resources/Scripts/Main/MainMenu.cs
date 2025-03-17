using Assets.Resources.Scripts.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.Main
{
    public class MainMenu : MonoBehaviour
    {
        public Button buttonLoad;
        public Button buttonNewGame;
        public Button buttonSettings;
        public Button buttonCloseLoadPanel;
        public GameObject loadPanel;

        void Start()
        {
            buttonLoad.onClick.AddListener(OnLoadButtonClick);
            buttonNewGame.onClick.AddListener(OnNewButtonClick);
            buttonSettings.onClick.AddListener(OnSettingsButtonClick);
            buttonCloseLoadPanel.onClick.AddListener(OnCloseLoadPanelClick);
        }

        void OnEnable()
        {
            loadPanel.SetActive(false);
            GameStatusManager.Instance.currentScene = GameStatusManager.CurrentScene.MAIN_MENU_SCENE;
        }

        void OnDisable()
        {
            loadPanel.SetActive(false);
        }

        void OnLoadButtonClick()
        {
            loadPanel.SetActive(true);
            GameLoadManager.Instance.LoadGame();
        }

        void OnNewButtonClick()
        {
            bool success = DataUtil.Instance.CreatePlayerData();
            if (success)
            {
                Debug.Log("New game created successfully");
                SceneManager.LoadScene("MainScene");
            }
            else
            {
                Debug.Log("Failed to create new game");
            }
        }

        void OnCloseLoadPanelClick()
        {
            loadPanel.SetActive(false);
        }

        void OnSettingsButtonClick()
        { }
    }
}