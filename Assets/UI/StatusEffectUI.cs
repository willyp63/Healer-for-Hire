using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectUI : MonoBehaviour
{
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private Image durationFill;

    [SerializeField]
    private TextMeshProUGUI stackText;

    public void UpdateDuration(float currentDuration, float maxDuration)
    {
        if (durationFill != null)
        {
            durationFill.fillAmount = currentDuration / maxDuration;
        }
    }

    public void UpdateStackCount(int count)
    {
        if (stackText != null)
        {
            stackText.gameObject.SetActive(count > 1);
            stackText.text = count.ToString();
        }
    }

    public void UpdateIcon(Sprite icon)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
        }
    }

    public void UpdateStatusEffect(
        Sprite icon,
        float currentDuration,
        float maxDuration,
        int stackCount
    )
    {
        UpdateIcon(icon);
        UpdateDuration(currentDuration, maxDuration);
        UpdateStackCount(stackCount);
    }
}
