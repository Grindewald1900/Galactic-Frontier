using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Planet
{
    public class PlanetDetailManager : MonoBehaviour
    {
        public static PlanetDetailManager Instance;
        public Image planetImage;
        public TextMeshProUGUI planetName;
        public TextMeshProUGUI planetLevel;
        public TextMeshProUGUI planetGenre;
        public TextMeshProUGUI planetDistance;
        public Button startButton;
        private PlanetEntity planetEntity;
        private readonly float spinSpeed = 10f;  // 扫描速度（度/秒）

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void Start()
        {
            Init();
        }

        void Update()
        {
            planetImage.rectTransform.Rotate(0, 0, -spinSpeed * Time.deltaTime);
        }

        private void Init()
        {
            if (startButton == null)
            {
                Debug.LogError("Start Button is null");
                return;
            }
            startButton.onClick.AddListener(() =>
            {
                BattleController.PendingBattleSeed = System.DateTime.UtcNow.Ticks;
                SceneManager.LoadScene("BattleScene");
            });
        }

        public void SetPlanetDetail(PlanetEntity planet)
        {
            planetEntity = planet;
            planetImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.planetImagePath, planet.backgroundSprite);
            planetName.text = planet.planetName;
            planetLevel.text = "Level:" + planet.planetLevel;
            planetGenre.text = "Genre" + planet.planetDescription;
            planetDistance.text = "Distance: 1000";
        }
    }
}