using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class Card : MonoBehaviour, IPointerClickHandler
{
    public UnityEngine.UI.Image healthBar;
    public UnityEngine.UI.Image energyBar;
    public GameObject baseObject;
    public Image baseColorImage;
    public Image characterImage;
    public Image logoImage;
    public Image backImage;
    public TextMeshProUGUI nameText;
    public DamageText[] damageTexts;
    public DebuffManager debuffManager;
    public BuffManager buffManager;
    public bool isPlayerCard;
    private bool isFlipped = false;  // **当前是否翻转**
    public CardEntity cardEntity;
    public CardExpertiseEntity cardExpertiseEntity;
    public CardBattleEntity cardBattleEntity;
    public delegate void CardClicked(Card card);
    public event CardClicked OnCardClicked;
    public float currentHealth = 100f;
    public float currentEnergy = 0f;
    public float currentAttack = 0f;
    public float progress = 0f;
    public int position = 0;
    public float moveDistance = 50f;
    public float forwardTime = 0.2f;
    private float backTime = 0.2f;
    private float attackTime = 0.3f;
    public float vanishDuration = 1f;
    public float highlightScale = 1.1f;
    public float animationDuration = 0.2f;
    private Vector3 startPos;
    private Vector3 originalScale;
    private int nextIndex = 0;
    private bool isHighlighted = false;

    public GameObject currentEffect;
    void Awake()
    {
        // gameObject.SetActive(false);
    }

    void Start()
    {
        startPos = transform.localPosition;
        originalScale = transform.localScale;
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
        SetName(cardEntity.characterName.ToString());
        FlipCard(false, false);
    }

    private void UpdateEnergyBar()
    {
        Debug.Log("IsbattleActive: " + IsBattleActive());
        if (!IsBattleActive()) return;
        if (BattleController.Instance.isSpecialAttackInProgress) return;
        progress = CalculateProgress(cardEntity.energyGenerateRate, cardEntity.maxEnergy);
        if (energyBar != null)
        {
            energyBar.fillAmount = progress;
        }
        if (progress >= 1f)
        {
            currentEnergy = cardEntity.maxEnergy;
        }
    }

    public void ResetEnergyBar()
    {
        currentEnergy = 0f;
        if (energyBar != null)
        {
            energyBar.fillAmount = 0f;
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

    private float CalculateProgress(float rate, float maxValue)
    {
        currentEnergy += rate * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxValue); // Ensure energy doesn't exceed max
        return currentEnergy / maxValue;
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

    public void FlipCard(bool isFlipped, bool isAnimated)
    {
        this.isFlipped = isFlipped;
        transform.DORotate(new Vector3(0, 90, 0), isAnimated ? 0.2f : 0f).OnComplete(() =>
        {
            baseObject.SetActive(!isFlipped);
            baseColorImage.gameObject.SetActive(!isFlipped);
            backImage.gameObject.SetActive(isFlipped);
            transform.DORotate(new Vector3(0, 180 * (isFlipped ? 1 : 0), 0), isAnimated ? 0.2f : 0f);
        });
    }

    public void Highlight()
    {
        if (isHighlighted)
            return;

        isHighlighted = true;
        transform.DOScale(originalScale * highlightScale, animationDuration).SetEase(Ease.OutBack);

        // if (outlineComponent != null)
        // {
        //     outlineComponent.enabled = true;
        // }
    }

    public void Unhighlight()
    {
        if (!isHighlighted)
            return;

        isHighlighted = false;
        // 恢复原始缩放
        transform.DOScale(originalScale, animationDuration).SetEase(Ease.InBack);
        // 关闭 Outline 边框
        // if (outlineComponent != null)
        // {
        //     outlineComponent.enabled = false;
        // }
    }
    public void SetImage(CardEntity cardEntity)
    {
        Sprite characterSprite = ImageUtil.GetSpriteByName(ImageUtil.characterImagePath, cardEntity.characterName.ToString() + "_01");
        Sprite baseColorSprite = ImageUtil.GetSpriteByName(ImageUtil.cardBkImagePath, cardEntity.characterTier.ToString() + "_Default");
        Sprite logoSprite = ImageUtil.GetSpriteByName(ImageUtil.badgeImagePath, cardEntity.characterTier.ToString());

        characterImage.sprite = characterSprite;
        baseColorImage.sprite = baseColorSprite;
        logoImage.sprite = logoSprite;
    }

    public void SetName(string name)
    {
        nameText.text = name;
    }

    public bool IsAlive()
    {
        return currentHealth > 0 && gameObject.activeSelf;
    }

    public bool IsBattleActive()
    {
        return SceneManager.GetActiveScene().name == "BattleScene";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsBattleActive() && OnCardClicked != null)
        {
            OnCardClicked(this);
        }
    }
}