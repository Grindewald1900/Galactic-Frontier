using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using DG.Tweening;

public class Card : MonoBehaviour
{
    public UnityEngine.UI.Image healthBar;
    public UnityEngine.UI.Image energyBar;
    public Image cardImage;
    public DamageText damageText;
    public string cardName;

    public bool isPlayerCard;
    public CardEntity cardEntity;
    public float currentHealth = 100f;
    public float currentEnergy = 0f;
    public float currentAttack = 0f;

    public int position = 0;

    public float moveDistance = 50f;
    public float forwardTime = 0.2f;
    public float backTime = 0.2f;
    private Vector3 startPos;
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

    private void UpdateAttack()
    {
        var progress = CalculateProgress(cardEntity.speed, cardEntity.maxAttack, ref currentAttack);
        if (progress >= 1f)
        {
            Attack();
        }

    }

    public bool IsAlive()
    {
        return currentHealth > 0 && gameObject.activeSelf;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log("Damage " + damage + " triggered.");
        ShowDamageText((Mathf.RoundToInt(damage)));
        currentHealth -= damage;
        currentAttack = Mathf.Clamp(currentHealth, 0, cardEntity.health);
        healthBar.fillAmount = currentHealth / cardEntity.health;
        if (currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }

    }

    private void ShowDamageText(int dmg)
    {
        damageText.SetDamageValue(dmg);
    }

    public void Attack()
    {
        PlayAttackAnimation();
        BattleController.Instance.AttackMinPosEnermy(this);
    }

    private void SuperAttack(float damage)
    {
        Debug.Log("SuperAttack " + damage + " triggered.");
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
}