using UnityEngine;

namespace Assets.Resources.Scripts.Main
{
    /// <summary>
    /// Stores lightweight navigation and activity flags that must survive scene changes.
    /// It does not load scenes or panels itself; scene and UI controllers publish their state here.
    /// </summary>
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

        /// <summary>The currently selected scene or main-menu panel.</summary>
        [field: SerializeField]
        public CurrentScene CurrentScene { get; set; } = CurrentScene.MAIN_MENU_SCENE;

        /// <summary>Indicates that the game is currently executing battle gameplay.</summary>
        [field: SerializeField]
        public bool IsBattle { get; set; } = false;

        /// <summary>Blocks incompatible menu navigation while the draw-card flow is open.</summary>
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

    /// <summary>
    /// Shared identifiers for both Unity scenes and panels inside MainScene.
    /// Values map to MainScrollController panel indices; do not reorder without migrating the scene.
    /// </summary>
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
