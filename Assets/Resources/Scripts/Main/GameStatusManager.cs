using UnityEngine;

namespace Assets.Resources.Scripts.Main
{
    public class GameStatusManager : MonoBehaviour
    {
        public static GameStatusManager Instance;
        public CurrentScene currentScene = CurrentScene.MAIN_MENU_SCENE;
        public bool isBattle = false;
        public bool isDrawingCard = false;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public enum CurrentScene
        {
            // Scene -> Menu -> SubMenu
            CHARACTER_MENU,
            CARDS_MENU,
            BATTLE_MENU,
            INVENTORY_MENU,
            BUILDING_MENU,
            SHOP_MENU,
            SETTINGS_MENU,
            DRAWCARDS_MENU,
            MAIN_MENU_SCENE,
            MAIN_SCENE,
            BATTLE_SCENE
        }
    }
}