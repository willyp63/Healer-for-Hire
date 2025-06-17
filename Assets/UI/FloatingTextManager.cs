using UnityEngine;

public class FloatingTextManager : Singleton<FloatingTextManager>
{
    [SerializeField]
    private FloatingText floatingTextPrefab;

    [SerializeField]
    private Canvas canvas;

    public void SpawnDamage(int damage, Vector3 position)
    {
        SpawnText(damage.ToString(), position, Color.red);
    }

    public void SpawnEffect(string effect, Vector3 position)
    {
        SpawnText(effect.ToUpper(), position, Color.yellow);
    }

    private void SpawnText(string text, Vector3 position, Color color)
    {
        FloatingText instance = Instantiate(floatingTextPrefab, canvas.transform);
        instance.Initialize(text, position, color);
    }
}
