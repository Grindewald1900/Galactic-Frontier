using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Resources.Scripts.Entity;

namespace Assets.Resources.Scripts.UI
{
    public class HoverShowDetailPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static HoverShowDetailPanel Instance;
        public GameObject detailPanel; // 需要显示/隐藏的编辑按钮
        public TextMeshProUGUI cardType;
        public TextMeshProUGUI level;
        public TextMeshProUGUI exp;
        public TextMeshProUGUI power;
        public TextMeshProUGUI score;
        public TextMeshProUGUI tier;
        public TextMeshProUGUI health;
        public TextMeshProUGUI attack;
        public TextMeshProUGUI defense;
        public TextMeshProUGUI accuracy;
        public TextMeshProUGUI dodge;
        public TextMeshProUGUI critical;
        public TextMeshProUGUI criticalDamage;
        public TextMeshProUGUI damageReduction;
        public TextMeshProUGUI energyGenerate;
        public TextMeshProUGUI speed;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            detailPanel?.SetActive(false);
        }

        public void SetDetailPanel(CardEntity cardEntity)
        {
            cardType.text = "Type: " + cardEntity.archetype.ToString();
            level.text = "Level: " + cardEntity.Level.ToString();
            exp.text = "Exp: " + cardEntity.CurrentExp.ToString() + "/" + cardEntity.ExpToNextLevel.ToString();
            power.text = "Power: " + cardEntity.power.ToString();
            score.text = "Score: " + cardEntity.score.ToString();
            tier.text = "Tier: " + cardEntity.CharacterTier.ToString();
            health.text = "Health: " + cardEntity.Health.ToString();
            attack.text = "Attack: " + cardEntity.Attack.ToString();
            defense.text = "Defense: " + cardEntity.Defense.ToString();
            accuracy.text = "Accuracy: " + cardEntity.Accuracy.ToString();
            dodge.text = "Dodge: " + cardEntity.Dodge.ToString();
            critical.text = "Crit Rate: " + cardEntity.Critical.ToString();
            criticalDamage.text = "Crit Damage: " + cardEntity.CriticalDamage.ToString();
            damageReduction.text = "Damage reduction: " + cardEntity.DamageReduction.ToString();
            energyGenerate.text = "Energy Generate: " + cardEntity.EnergyGenerateRate.ToString();
            speed.text = "Speed: " + cardEntity.Speed.ToString();
        }

        public void ShowCardDetailPanel()
        {
            detailPanel?.SetActive(true);
        }

        public void HideCardDetailPanel()
        {
            detailPanel?.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowCardDetailPanel();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideCardDetailPanel();
        }
    }
}