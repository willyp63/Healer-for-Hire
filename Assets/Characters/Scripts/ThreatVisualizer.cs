using System.Collections.Generic;
using UnityEngine;

public class ThreatVisualizer : MonoBehaviour
{
    [SerializeField]
    private Material lineMaterial;

    [SerializeField]
    private Color highThreatColor = Color.red;

    [SerializeField]
    private Color lowThreatColor = Color.yellow;

    [SerializeField]
    private float lineWidth = 0.05f;

    [SerializeField]
    private float lineOpacity = 0.7f; // Opacity of the threat lines (0-1)

    [SerializeField]
    private float lineOffset = 0.5f; // Offset from character center

    [SerializeField]
    private bool animateLines = true;

    [SerializeField]
    private float animationSpeed = 1f;

    private Dictionary<Character, LineRenderer> threatLines =
        new Dictionary<Character, LineRenderer>();
    private CharacterManager characterManager;
    private float animationTime = 0f;

    private void Start()
    {
        characterManager = CharacterManager.Instance;
        if (characterManager == null)
        {
            Debug.LogError("ThreatVisualizer: CharacterManager not found!");
            return;
        }

        // Create line material if not assigned
        if (lineMaterial == null)
        {
            CreateDefaultLineMaterial();
        }
    }

    private void CreateDefaultLineMaterial()
    {
        // Create a material that supports transparency
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            lineMaterial = new Material(shader);
            lineMaterial.color = lowThreatColor; // Use lowThreatColor as default
        }
        else
        {
            // Fallback to Unlit/Transparent if Sprites/Default is not available
            shader = Shader.Find("Unlit/Transparent");
            if (shader != null)
            {
                lineMaterial = new Material(shader);
                lineMaterial.color = lowThreatColor; // Use lowThreatColor as default
            }
            else
            {
                Debug.LogWarning(
                    "ThreatVisualizer: Could not find a shader that supports transparency for line material"
                );
            }
        }
    }

    private void Update()
    {
        if (characterManager == null)
            return;

        animationTime += Time.deltaTime * animationSpeed;
        UpdateThreatLines();
    }

    private void UpdateThreatLines()
    {
        var enemyCharacters = characterManager.GetActiveEnemyCharacters();

        // Remove lines for dead enemies
        var enemiesToRemove = new List<Character>();
        foreach (var kvp in threatLines)
        {
            if (!enemyCharacters.Contains(kvp.Key) || kvp.Key == null)
            {
                enemiesToRemove.Add(kvp.Key);
            }
        }

        foreach (var enemy in enemiesToRemove)
        {
            RemoveThreatLine(enemy);
        }

        // Update or create lines for active enemies
        foreach (var enemy in enemyCharacters)
        {
            if (enemy == null)
                continue;

            Character highestThreatTarget = enemy.GetHighestThreatTarget();

            if (highestThreatTarget != null && highestThreatTarget != enemy)
            {
                CreateOrUpdateThreatLine(enemy, highestThreatTarget);
            }
            else
            {
                RemoveThreatLine(enemy);
            }
        }
    }

    private void CreateOrUpdateThreatLine(Character enemy, Character target)
    {
        if (!threatLines.ContainsKey(enemy))
        {
            // Create new line renderer
            GameObject lineObject = new GameObject($"ThreatLine_{enemy.name}");
            lineObject.transform.SetParent(transform);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 0;
            lineRenderer.sortingLayerName = "Background";

            // Add some visual effects
            if (animateLines)
            {
                lineRenderer.textureMode = LineTextureMode.Tile;
                lineRenderer.material.mainTextureScale = new Vector2(1f, 1f);
            }

            threatLines[enemy] = lineRenderer;
        }

        // Update line positions
        LineRenderer line = threatLines[enemy];
        Vector3 enemyPos = enemy.transform.position + Vector3.up * lineOffset;
        Vector3 targetPos = target.transform.position + Vector3.up * lineOffset;

        line.SetPosition(0, enemyPos);
        line.SetPosition(1, targetPos);

        // Update line color based on threat difference
        Color threatColor = GetThreatColor(enemy.ThreatTable, target);
        line.startColor = threatColor;
        line.endColor = threatColor;

        // Animate line texture offset for flowing effect
        if (animateLines && line.material != null)
        {
            Vector2 offset = line.material.mainTextureOffset;
            offset.x = animationTime;
            line.material.mainTextureOffset = offset;
        }
    }

    private void RemoveThreatLine(Character enemy)
    {
        if (threatLines.ContainsKey(enemy))
        {
            if (threatLines[enemy] != null)
            {
                Destroy(threatLines[enemy].gameObject);
            }
            threatLines.Remove(enemy);
        }
    }

    private Color GetThreatColor(
        Dictionary<Character, float> threatTable,
        Character highestThreatTarget
    )
    {
        if (threatTable == null)
        {
            Color lowColor = lowThreatColor;
            lowColor.a = lineOpacity;
            return lowColor;
        }

        if (threatTable.Count == 1)
        {
            Color highColor = highThreatColor;
            highColor.a = lineOpacity;
            return highColor;
        }

        // Get the highest and second highest threat values
        float highestThreat = 0f;
        float secondHighestThreat = 0f;

        foreach (var kvp in threatTable)
        {
            if (kvp.Value > highestThreat)
            {
                secondHighestThreat = highestThreat;
                highestThreat = kvp.Value;
            }
            else if (kvp.Value > secondHighestThreat)
            {
                secondHighestThreat = kvp.Value;
            }
        }

        // Calculate the threat difference ratio
        float threatDifference = highestThreat - secondHighestThreat;
        float threatRatio = secondHighestThreat > 0f ? highestThreat / secondHighestThreat : 2f; // If no second threat, treat as 2x

        // Determine color based on threat ratio
        Color baseColor;
        if (threatRatio >= 2f)
        {
            // Red when highest threat is at least 2x the second highest
            baseColor = highThreatColor;
        }
        else if (threatRatio <= 1.1f)
        {
            // Yellow when threat is very close (within 10%)
            baseColor = lowThreatColor;
        }
        else
        {
            // Interpolate between yellow and red for values in between
            float t = (threatRatio - 1.1f) / (2f - 1.1f); // Normalize to 0-1 range
            baseColor = Color.Lerp(lowThreatColor, highThreatColor, t);
        }

        // Apply opacity
        baseColor.a = lineOpacity;
        return baseColor;
    }

    public void SetLineVisibility(bool visible)
    {
        foreach (var line in threatLines.Values)
        {
            if (line != null)
            {
                line.enabled = visible;
            }
        }
    }

    public void ClearAllLines()
    {
        foreach (var line in threatLines.Values)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
        threatLines.Clear();
    }

    public void SetAnimationEnabled(bool enabled)
    {
        animateLines = enabled;
    }

    private void OnDestroy()
    {
        ClearAllLines();
    }
}
