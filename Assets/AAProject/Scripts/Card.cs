using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class Card : MonoBehaviour
{
    public UnityEngine.UI.Image healthBar;
    public UnityEngine.UI.Image energyBar;
    public Image cardImage;
    public string cardName;
    public float damage = 10;
    public float speed = 10;
    public float energyIncreaseRate = 10f;
    public float maxHealth = 100f;
    public float maxEnergy = 100f;
    public float maxAttack = 30f;
    public float currentHealth = 100f;
    public float currentEnergy = 0f;
    public float currentAttack = 0f;

    public int position = 0;

    void Awake()
    {
        gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        UpdateEnergyBar();
        UpdateAttack();
    }

    public void InitCard(CardEntity cardEntity)
    {

    }
    private void UpdateEnergyBar()
    {
        var progress = CalculateProgress(energyIncreaseRate, maxEnergy, ref currentEnergy);
        if (energyBar != null)
        {
            energyBar.fillAmount = progress;
        }
        if (progress >= 1f)
        {
            currentEnergy = 0f;
            SuperAttack(damage * 2);
        }
    }

    private void UpdateAttack()
    {
        var progress = CalculateProgress(speed, maxAttack, ref currentAttack);
        if (progress >= 1f)
        {
            Attack(damage);
        }
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    private void TakeDamage(float damage)
    {
        Debug.Log("Damage " + damage + " triggered.");
        currentHealth -= damage;
        currentAttack = Mathf.Clamp(currentHealth, 0, maxHealth);
        healthBar.fillAmount = currentHealth / maxHealth;
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void Attack(float damage)
    {
        Debug.Log("Attack " + damage + " triggered.");
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
}