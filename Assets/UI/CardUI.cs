using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [SerializeField]
    private Image artImage;

    [SerializeField]
    private GameObject resourceCostDisplay;

    [SerializeField]
    private TextMeshProUGUI resourceText;

    [SerializeField]
    private GameObject castTimeDisplay;

    [SerializeField]
    private TextMeshProUGUI castTimeText;

    public void SetCard(Card card)
    {
        if (card == null)
        {
            artImage.sprite = null;
            artImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            resourceCostDisplay.SetActive(false);
            castTimeDisplay.SetActive(false);
            return;
        }

        artImage.sprite = card.cardArt;

        resourceCostDisplay.SetActive(card.resourceCost > 0f);
        resourceText.text = card.resourceCost.ToString();

        castTimeDisplay.SetActive(card.castTime > 0f);
        castTimeText.text = card.castTime.ToString() + "s";
    }
}
