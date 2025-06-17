using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitFrameUI : MonoBehaviour
{
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

    public void SetPortrait(Sprite portraitSprite)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = portraitSprite;
        }
    }
}
