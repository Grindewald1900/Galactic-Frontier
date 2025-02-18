using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Card : MonoBehaviour
{
    public UnityEngine.UI.Image healthBar;
    public UnityEngine.UI.Image energyBar;
    public Image baseColorImage;
    public Image characterImage;
    public Image logoImage;
    public TextMeshProUGUI nameText;
    public DamageText[] damageTexts;
    public DebuffManager debuffManager;
    public BuffManager buffManager;
    public bool isPlayerCard;
    public CardEntity cardEntity;
    public CardExpertiseEntity cardExpertiseEntity;
    public CardBattleEntity cardBattleEntity;
    public float currentHealth = 100f;
    public float currentEnergy = 0f;
    public float currentAttack = 0f;
    public int position = 0;
    public float moveDistance = 50f;
    public float forwardTime = 0.2f;
    private float backTime = 0.2f;
    private float attackTime = 0.3f;
    public float vanishDuration = 1f;
    private Vector3 startPos;
    private int nextIndex = 0;

    public GameObject currentEffect;
    void Awake()
    {
        gameObject.SetActive(false);
    }

    void Start()
    {
        startPos = transform.localPosition;
        energyBar.fillAmount = 0f;
        currentEffect.SetActive(true);
    }

    void Update()
    {
        UpdateEnergyBar();
    }

    public void InitCard(CardEntity cardEntity)
    {
        this.cardEntity = cardEntity;
        cardExpertiseEntity = new CardExpertiseEntity();
        cardBattleEntity = new CardBattleEntity();
        SetImage(cardEntity);
        SetName(cardEntity.cardName);
    }

    private void UpdateEnergyBar()
    {
        if (BattleInfo.Instance.isBattleInfoActive) return;
        if (BattleController.Instance.isSpecialAttackInProgress) return;
        var progress = CalculateProgress(cardEntity.energyGenerateRate, cardEntity.maxEnergy, ref currentEnergy);
        if (energyBar != null)
        {
            energyBar.fillAmount = progress;
        }
        if (progress >= 1f)
        {
            currentEnergy = 0f;
            Debug.Log("SpecialAttack: " + cardEntity.cardName);
            BattleController.Instance.isSpecialAttackInProgress = true;
            SkillSet skillSet = CharacterSkillController.GetSkillSet(cardEntity.character);
            skillSet.SpecialAttack(this, BattleController.Instance.GetAliveTargets(this));
        }
    }

    public void TakeDamage(List<DamageEntity> damage)
    {
        StartCoroutine(ApplyDamage(damage));
        CardEffectManager.Instance.SpawnHitEffect(this, new Vector2(0, 0));
    }

    private IEnumerator ApplyDamage(List<DamageEntity> damage)
    {
        Debug.Log("ApplyDamage: " + damage.Count);
        if (damageTexts == null || damageTexts.Length == 0)
        {
            Debug.LogWarning("DamageTextManager: damageTexts is null");
            yield break;
        }
        foreach (var d in damage)
        {
            DamageText dt = damageTexts[nextIndex];
            nextIndex = (nextIndex + 1) % damageTexts.Length;
            string text = "";
            Color textColor = Color.white;
            if (d.damageType == DamageType.MISS)
            {
                text = "MISS";
                textColor = ColorUtil.missDamagelColor;
            }
            else if (d.damageType == DamageType.DAMAGE)
            {
                text = $"-{Mathf.RoundToInt(d.damageAmount)}";
                textColor = d.criticalMultiplier > 1 ? ColorUtil.criticalDamagelColor : ColorUtil.plainDamagelColor;
            }
            else if (d.damageType == DamageType.SPECIAL_DAMAGE)
            {
                text = $"-{Mathf.RoundToInt(d.damageAmount)}";
                textColor = d.criticalMultiplier > 1 ? ColorUtil.criticalDamagelColor : ColorUtil.plainDamagelColor;
            }

            dt.SetDamageText(text, textColor);
            currentHealth -= d.damageAmount;
            yield return new WaitForSeconds(attackTime);
        }
        nextIndex = 0;
        healthBar.fillAmount = currentHealth / cardEntity.health;
        if (damage[0].damageType == DamageType.SPECIAL_DAMAGE)
        {
            BattleController.Instance.isSpecialAttackInProgress = false;
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOScale(Vector3.zero, vanishDuration).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private float CalculateProgress(float rate, float maxValue, ref float currentValue)
    {
        if (currentValue < maxValue)
        {
            currentValue += rate * Time.deltaTime;
            currentValue = Mathf.Clamp(currentValue, 0, maxValue); // Ensure energy doesn't exceed max
        }
        else
        {
            currentValue = 0f;
        }
        return currentValue / maxValue;
    }

    public void PlayAttackAnimation()
    {
        Vector3 attackPos = isPlayerCard ? startPos + new Vector3(moveDistance, 0, 0) : startPos - new Vector3(moveDistance, 0, 0);
        transform.DOLocalMove(attackPos, forwardTime)
                 .OnComplete(() =>
                 {
                     transform.DOLocalMove(startPos, backTime);
                 });
    }

    public void SetImage(CardEntity cardEntity)
    {
        Sprite characterSprite = ImageUtil.GetSpriteByName(ImageUtil.characterImagePath, cardEntity.character.ToString() + "_01");
        Sprite baseColorSprite = ImageUtil.GetSpriteByName(ImageUtil.cardBkImagePath, cardEntity.characterTier.ToString() + "_Default");
        // Sprite logoSprite = ImageUtil.GetSpriteByName(ImageUtil.logoImagePath, cardEntity.cardName + "_01");

        characterImage.sprite = characterSprite;
        baseColorImage.sprite = baseColorSprite;
        // logoImage.sprite = logoSprite;
    }

    public void SetName(string name)
    {
        nameText.text = name;
    }

    public bool IsAlive()
    {
        return currentHealth > 0 && gameObject.activeSelf;
    }
}