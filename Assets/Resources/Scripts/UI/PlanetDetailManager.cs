using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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

    private void Init()
    {
        if (startButton == null)
        {
            Debug.LogError("Start Button is null");
            return;
        }
        startButton.onClick.AddListener(() =>
        {
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
        planetDistance.text = "Distance: " + "1000";
    }
}