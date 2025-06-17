using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI mainText;

    [SerializeField]
    private TextMeshProUGUI[] textComponents;

    [SerializeField]
    private float lifetime = 2f;

    [SerializeField]
    private float riseSpeed = 2f;

    [SerializeField]
    private AnimationCurve fadeCurve;

    private float timer;
    private Vector3 startPosition;

    public void Initialize(string text, Vector3 worldPosition, Color color)
    {
        foreach (var textComponent in textComponents)
        {
            textComponent.text = text;
            if (mainText == textComponent)
                textComponent.color = color;
        }
        transform.position = worldPosition;
        startPosition = worldPosition;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Move upward
        transform.position = startPosition + Vector3.up * (riseSpeed * timer);

        // Fade out
        float alpha = fadeCurve.Evaluate(timer / lifetime);
        foreach (var textComponent in textComponents)
        {
            Color color = textComponent.color;
            color.a = alpha;
            textComponent.color = color;
        }

        // Destroy when done
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
