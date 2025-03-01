using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestCardDamage : MonoBehaviour, IPointerClickHandler
{
    public UnityEngine.UI.Image healthBar;
    public UnityEngine.UI.Image energyBar;

    public float damage = 10;
    public float energyIncreaseRate = 10f;
    public float maxHealth = 100f;
    public float maxEnergy = 100f;
    public float healthAmount = 100f;
    public float energyAmount = 100f;

    public void OnPointerClick(PointerEventData eventData)
    {
        // Trigger the Damage function when the panel is clicked
        Damage();
    }

    // The Damage function
    private void Damage()
    {
        Debug.Log("Panel clicked! Damage triggered.");

        // Add your damage logic here
        healthAmount -= damage;
        healthBar.fillAmount = healthAmount / maxHealth;
    }
    private void UpdateEnergyBar()
    {
        if (energyBar != null)
        {
            energyBar.fillAmount = energyAmount / maxEnergy;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Increase energy over time
        if (energyAmount < maxEnergy)
        {
            energyAmount += energyIncreaseRate * Time.deltaTime;
            energyAmount = Mathf.Clamp(energyAmount, 0, maxEnergy); // Ensure energy doesn't exceed max
            UpdateEnergyBar();
        }
        else
        {
            // Energy is full, release the skill
            // ReleaseSkill();
            energyAmount = 0f; // Reset energy after skill is released
        }
    }
}
