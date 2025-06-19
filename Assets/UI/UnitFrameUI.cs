using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UnitFrameHighlightStatus
{
    SelectedAndHovered,
    Selected,
    Hovered,
    None,
}

public class UnitFrameUI : MonoBehaviour
{
    [SerializeField]
    private Image highlightImage;

    [SerializeField]
    private Image portraitImage;

    [SerializeField]
    private Image healthFill;

    [SerializeField]
    private TextMeshProUGUI healthText;

    [SerializeField]
    private Image resourceFill;

    [SerializeField]
    private TextMeshProUGUI resourceText;

    [SerializeField]
    private Transform statusEffectsContainer;

    [SerializeField]
    private StatusEffectUI statusEffectPrefab;

    private Dictionary<StatusEffect, StatusEffectUI> activeStatusEffects =
        new Dictionary<StatusEffect, StatusEffectUI>();

    public void SetHealth(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }

        if (healthFill != null)
        {
            healthFill.transform.localScale = new Vector3(
                maxHealth > 0 ? (float)currentHealth / maxHealth : 0f,
                1,
                1
            );
        }
    }

    public void SetResource(int currentResource, int maxResource, ResourceType resourceType)
    {
        if (resourceText != null)
        {
            resourceText.text = $"{currentResource} / {maxResource}";
        }

        if (resourceFill != null)
        {
            resourceFill.transform.localScale = new Vector3(
                maxResource > 0 ? (float)currentResource / maxResource : 0f,
                1,
                1
            );

            // Set color based on resource type
            switch (resourceType)
            {
                case ResourceType.Mana:
                    resourceFill.color = new Color(31f / 255f, 127f / 255f, 153f / 255f, 1f);
                    break;
                case ResourceType.Rage:
                    resourceFill.color = new Color(153f / 255f, 31f / 255f, 31f / 255f, 1f);
                    break;
                case ResourceType.Energy:
                    resourceFill.color = new Color(153f / 255f, 153f / 255f, 31f / 255f, 1f);
                    break;
                default:
                    resourceFill.color = Color.white;
                    break;
            }
        }
    }

    public void SetHighlightStatus(UnitFrameHighlightStatus highlightStatus)
    {
        if (highlightImage != null)
        {
            var alpha = 0f;
            if (highlightStatus == UnitFrameHighlightStatus.SelectedAndHovered)
            {
                alpha = 0.9f;
            }
            else if (highlightStatus == UnitFrameHighlightStatus.Selected)
            {
                alpha = 0.75f;
            }
            else if (highlightStatus == UnitFrameHighlightStatus.Hovered)
            {
                alpha = 0.25f;
            }

            highlightImage.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    public void SetPortrait(Sprite portraitSprite)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = portraitSprite;
        }
    }

    public void UpdateStatusEffects(IReadOnlyList<StatusEffect> statusEffects)
    {
        // Create a list to track which status effects should be removed
        List<StatusEffect> statusEffectsToRemove = new List<StatusEffect>(activeStatusEffects.Keys);

        // Update or create status effects
        foreach (var statusEffect in statusEffects)
        {
            statusEffectsToRemove.Remove(statusEffect);

            if (activeStatusEffects.TryGetValue(statusEffect, out StatusEffectUI statusEffectUI))
            {
                // Update existing status effect
                statusEffectUI.UpdateStatusEffect(
                    statusEffect.EffectIcon,
                    statusEffect.TimeRemaining,
                    statusEffect.Duration,
                    statusEffect.CurrentStacks
                );
            }
            else
            {
                // Create new status effect
                StatusEffectUI newStatusEffectUI = Instantiate(
                    statusEffectPrefab,
                    statusEffectsContainer
                );
                newStatusEffectUI.UpdateStatusEffect(
                    statusEffect.EffectIcon,
                    statusEffect.TimeRemaining,
                    statusEffect.Duration,
                    statusEffect.CurrentStacks
                );
                activeStatusEffects[statusEffect] = newStatusEffectUI;
            }
        }

        // Remove status effects that are no longer active
        foreach (var type in statusEffectsToRemove)
        {
            if (activeStatusEffects.TryGetValue(type, out StatusEffectUI statusEffectUI))
            {
                Destroy(statusEffectUI.gameObject);
                activeStatusEffects.Remove(type);
            }
        }
    }
}
