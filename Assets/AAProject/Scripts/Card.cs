using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class Card : MonoBehaviour
{
    public UnityEngine.UI.Image healthBar;
    public UnityEngine.UI.Image energyBar;
    public Image cardImage;
    public DamageText[] damageTexts;
    public string cardName;

    public bool isPlayerCard;
    public CardEntity cardEntity;
    public float currentHealth = 100f;
    public float currentEnergy = 0f;
    public float currentAttack = 0f;

    public int position = 0;

    public float moveDistance = 50f;
    public float forwardTime = 0.2f;
    private float backTime = 0.2f;
    private float attackTime = 0.2f;
    public float vanishDuration = 1f;
    private Vector3 startPos;
    private int nextIndex = 0;
    void Awake()
    {
        gameObject.SetActive(false);
    }

    void Start()
    {
        startPos = transform.localPosition;
    }
    // Update is called once per frame
    void Update()

    {
        UpdateEnergyBar();
        // UpdateAttack();
    }

    public void InitCard(CardEntity cardEntity)
    {
        this.cardEntity = cardEntity;
    }

    private void UpdateEnergyBar()
    {
        var progress = CalculateProgress(cardEntity.energyGenerateRate, cardEntity.maxEnergy, ref currentEnergy);
        if (energyBar != null)
        {
            energyBar.fillAmount = progress;
        }
        if (progress >= 1f)
        {
            currentEnergy = 0f;
            SuperAttack(cardEntity.attack * 2);
        }
    }

    public IEnumerator TakeDamage(DamageEntity[] damage)
    {
        if (damageTexts == null || damageTexts.Length == 0)
        {
            Debug.LogWarning("DamageTextManager: damageTexts is null");
            yield break;
        }
        foreach (var d in damage)
        {
            if (d.damageType == DamageType.MISS)
            {
                damageTexts[nextIndex].SetDamageText("MISS", ColorUtil.missDamagelColor);
                continue;
            }
            DamageText dt = damageTexts[nextIndex];
            nextIndex = (nextIndex + 1) % damageTexts.Length;
            Color textColor = d.criticalMultiplier > 1 ? ColorUtil.criticalDamagelColor : ColorUtil.plainDamagelColor;
            dt.SetDamageText($"-{Mathf.RoundToInt(d.damageAmount)}", textColor);
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

    private void SuperAttack(float damage)
    {
        // Debug.Log("SuperAttack " + damage + " triggered.");
    }
    public void Die()
    {
        // CanvasGroup cg = GetComponent<CanvasGroup>();
        // if (cg == null)
        // {
        //     cg = gameObject.AddComponent<CanvasGroup>();
        // }

        // 创建 DOTween 动画序列，同时进行淡出和缩小效果
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

    public bool IsAlive()
    {
        return currentHealth > 0 && gameObject.activeSelf;
    }
}