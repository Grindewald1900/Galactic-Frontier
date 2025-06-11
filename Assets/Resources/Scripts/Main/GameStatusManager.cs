using UnityEngine;

namespace Assets.Resources.Scripts.Main
{
    public class GameStatusManager : MonoBehaviour
    {
        #region Singleton Instance
        public static GameStatusManager Instance
        {
            get
            {
                if (instance == null)
                    instance = Object.FindAnyObjectByType<GameStatusManager>();
                return instance;
            }
        }
        private static GameStatusManager instance;
        #endregion

        [field: SerializeField]
        public CurrentScene CurrentScene { get; set; } = CurrentScene.MAIN_MENU_SCENE;
        [field: SerializeField]
        public bool IsBattle { get; set; } = false;

        [field: SerializeField]
        public bool IsDrawingCard { get; set; }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
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
