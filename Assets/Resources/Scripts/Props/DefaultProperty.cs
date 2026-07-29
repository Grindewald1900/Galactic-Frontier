using System.Numerics;

namespace Assets.Resources.Scripts.Props
{
    public static class DefaultProperty
    {
        public const float defaultCanvasScale = 0.0093f;
        public static readonly Vector2 screenSize = new Vector2(1920, 1080);
        public const float defaultHealth = 100f;
        public const float defaultAttack = 10f;
        public const float defaultDefense = 10f;
        public const int defaultLineupSize = 5;
        public const float defaultAttackTime = 1.5f;
        public const int defaultBuffRound = 3;
        public const int defaultDebuffRound = 3;
        public const int defaultControllRound = 1;
        public const float defaultCardScale = 1.1f;
        public const float highlightCardScale = 1.2f;

        // Characters
        public const int MAX_LEVEL = 200; // Current max Level

        //Strings
        public const string AVATAR_PATH = "AvatarPath";
        public const string PLAYER_ID = "PlayerID";

        public const string PLAYER_ENTITIES = "/playerEntities.json";
        public const string AVATAR = "/avatar.png";
        public const string PLAYER_DATA = "/playerData.json";
        public const string PLAYER_CARDS_DATA = "/playerCards.json";
        public const string ITEM_DATA = "/itemData.json";
        public const string EXPERT_DATA = "/expertData.json";

        // Paths
        public const string SKILL_DATA_PATH = "data/SkillData_encrypted";
        public const string BASE_ATTR_PATH = "data/BaseAttributes";

        // Configs
        public static bool isDebug = true;

        // Gift code
        public const string CODE_DEBUG_BOARD = "000";
    }
}
