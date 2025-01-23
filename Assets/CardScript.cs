using UnityEngine;

public class CardScript : MonoBehaviour
{
    public UnityEngine.UI.Image healthBar;
    public UnityEngine.UI.Image energyBar;
    public float damage = 10;
    public float speed = 10;
    public float energyIncreaseRate = 10f;
    public float maxHealth = 100f;
    public float maxEnergy = 100f;

    public float maxAttack = 30f;

    public float healthAmount = 100f;
    public float energyAmount = 0f;
    public float attackAmount = 0f;

    private int position = 1;

    // The Damage function
    private void TakeDamage(float damage)
    {
        Debug.Log("Damage " + damage + " triggered.");

        // Add your damage logic here
        healthAmount -= damage;
        healthBar.fillAmount = healthAmount / maxHealth;
    }

    private void Attack(float damage)
    {
        Debug.Log("Attack " + damage + " triggered.");

    }

    private void SuperAttack(float damage)
    {
        Debug.Log("SuperAttack " + damage + " triggered.");

    }

    private void UpdateEnergyBar()
    {
        var progress = CalculateProgress(energyIncreaseRate, maxEnergy, ref energyAmount);
        if (energyBar != null)
        {
            energyBar.fillAmount = progress;
        }
        if (progress >= 1f)
        {
            energyAmount = 0f;
            SuperAttack(damage * 2);
        }
    }

    private void UpdateAttack()
    {
        var progress = CalculateProgress(speed, maxAttack, ref attackAmount);
        if (progress >= 1f)
        {
            Attack(damage);
        }
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

    // Update is called once per frame
    void Update()
    {
        UpdateEnergyBar();
        UpdateAttack();
    }
}