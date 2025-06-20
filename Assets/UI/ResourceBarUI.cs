using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBarUI : MonoBehaviour
{
    [SerializeField]
    private Image fillImage;

    [SerializeField]
    private TextMeshProUGUI text;

    public void SetResource(int currentResource, int maxResource)
    {
        fillImage.transform.localScale = new Vector3((float)currentResource / maxResource, 1, 1);
        text.text = currentResource.ToString() + " / " + maxResource.ToString();
    }
}
