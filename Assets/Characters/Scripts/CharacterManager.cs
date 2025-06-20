using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterManager : Singleton<CharacterManager>
{
    [SerializeField]
    private Transform background;

    [SerializeField]
    private List<Character> playerCharacters = new();

    [SerializeField]
    private List<CharacterSlot> playerSlots;

    [SerializeField]
    private List<CharacterSlot> enemySlots;

    [SerializeField]
    private List<UnitFrameUI> playerFrames;

    [SerializeField]
    private List<UnitFrameUI> enemyFrames;

    [SerializeField]
    private ThreatVisualizer threatVisualizer;

    private Character mainPlayerCharacter;
    public Character MainPlayerCharacter => mainPlayerCharacter;

    private Character[] activePlayerCharacters;
    private Character[] activeEnemyCharacters;

    // Mapping between characters and their UnitFrameUI
    private Dictionary<Character, UnitFrameUI> characterToUnitFrame = new();

    // Update interval for UI
    private const float UI_UPDATE_INTERVAL = 0.1f;

    private const float WAVE_DISTANCE = 8.8888f * 2f;

    [SerializeField]
    private float waveMovementSpeed = 2f;
    public float WaveMovementSpeed => waveMovementSpeed;

    private Vector3 originalBackgroundPosition;
    private bool isWaveInProgress = false;
    public bool IsWaveInProgress => isWaveInProgress;

    private void Start()
    {
        activePlayerCharacters = new Character[playerSlots.Count];
        activeEnemyCharacters = new Character[enemySlots.Count];

        // Store original background position
        if (background != null)
        {
            originalBackgroundPosition = background.position;
        }

        InitializeBattle();
        StartCoroutine(UIUpdateLoop());
    }

    private void InitializeBattle()
    {
        // Spawn player characters
        int playerCount = Mathf.Min(playerCharacters.Count, playerSlots.Count);
        int playerStartIndex = (playerSlots.Count - playerCount) / 2;

        for (int i = 0; i < playerCount; i++)
        {
            if (playerCharacters[i].IsEnemy)
                Debug.LogError("Player character is enemy");

            var slotIndex = playerStartIndex + i;
            var character = Instantiate(
                playerCharacters[i],
                playerSlots[slotIndex].transform.position,
                Quaternion.identity
            );
            activePlayerCharacters[slotIndex] = character;

            // Assign corresponding UnitFrameUI
            if (slotIndex < playerFrames.Count)
            {
                characterToUnitFrame[character] = playerFrames[slotIndex];
                InitializeUnitFrame(character, playerFrames[slotIndex]);
            }

            if (character.IsMainPlayer)
            {
                mainPlayerCharacter = character;
            }
        }

        // Hide unused UnitFrames
        HideUnusedPlayerUnitFrames();
        HideUnusedEnemyUnitFrames();
    }

    private void HideUnusedPlayerUnitFrames()
    {
        // Hide unused player UnitFrames
        for (int i = 0; i < playerFrames.Count; i++)
        {
            playerFrames[i].gameObject.SetActive(activePlayerCharacters[i] != null);
        }
    }

    private void HideUnusedEnemyUnitFrames()
    {
        // Hide unused enemy UnitFrames
        for (int i = 0; i < enemyFrames.Count; i++)
        {
            enemyFrames[i].gameObject.SetActive(activeEnemyCharacters[i] != null);
        }
    }

    private void HideEnemyUnitFrames()
    {
        // Hide unused enemy UnitFrames
        for (int i = 0; i < enemyFrames.Count; i++)
        {
            enemyFrames[i].gameObject.SetActive(false);
        }
    }

    private void InitializeUnitFrame(Character character, UnitFrameUI unitFrame)
    {
        // Set initial health and resource values
        unitFrame.SetHealth(character.CurrentHealth, character.MaxHealth);
        unitFrame.SetResource(
            character.CurrentResource,
            character.MaxResource,
            character.ResourceType
        );

        // Set portrait if available
        unitFrame.SetPortrait(character.PortraitSprite);
    }

    private IEnumerator UIUpdateLoop()
    {
        while (true)
        {
            UpdateAllUnitFrames();
            yield return new WaitForSeconds(UI_UPDATE_INTERVAL);
        }
    }

    private void UpdateAllUnitFrames()
    {
        foreach (var kvp in characterToUnitFrame)
        {
            var character = kvp.Key;
            var unitFrame = kvp.Value;

            if (character != null && unitFrame != null)
            {
                unitFrame.SetHealth(character.CurrentHealth, character.MaxHealth);
                unitFrame.SetResource(
                    character.CurrentResource,
                    character.MaxResource,
                    character.ResourceType
                );
                unitFrame.UpdateStatusEffects(character.ActiveEffects);
            }
        }
    }

    public List<Character> GetActivePlayerCharacters() => new(activePlayerCharacters);

    public List<Character> GetActiveEnemyCharacters() => new(activeEnemyCharacters);

    public void RemoveCharacter(Character character)
    {
        var activeCharacterIndex = Array.IndexOf(activePlayerCharacters, character);

        if (activeCharacterIndex != -1)
        {
            activePlayerCharacters[activeCharacterIndex] = null;
        }
        else
        {
            var activeEnemyCharacterIndex = Array.IndexOf(activeEnemyCharacters, character);
            if (activeEnemyCharacterIndex != -1)
            {
                activeEnemyCharacters[activeEnemyCharacterIndex] = null;
                // Hide enemy UnitFrame when they die
                if (activeEnemyCharacterIndex < enemyFrames.Count)
                {
                    enemyFrames[activeEnemyCharacterIndex].gameObject.SetActive(false);
                }
            }
        }

        // Remove from UnitFrame mapping
        characterToUnitFrame.Remove(character);

        Destroy(character.gameObject);
    }

    public Vector3 GetCharacterSlotPosition(Character character)
    {
        // Search in player characters first
        int playerIndex = Array.IndexOf(activePlayerCharacters, character);
        if (playerIndex != -1 && playerIndex < playerSlots.Count)
        {
            return playerSlots[playerIndex].transform.position;
        }

        // Search in enemy characters
        int enemyIndex = Array.IndexOf(activeEnemyCharacters, character);
        if (enemyIndex != -1 && enemyIndex < enemySlots.Count)
        {
            return enemySlots[enemyIndex].transform.position;
        }

        // If character not found, return zero vector
        return Vector3.zero;
    }

    public Character GetClosestTarget(Character character)
    {
        // Get the appropriate lists based on which side the character is on
        var sourceList = character.IsEnemy ? activeEnemyCharacters : activePlayerCharacters;
        var targetList = character.IsEnemy ? activePlayerCharacters : activeEnemyCharacters;

        int characterIndex = Array.IndexOf(sourceList, character);

        if (characterIndex == -1)
            return null;

        // Try direct opposite first
        if (characterIndex < targetList.Length && targetList[characterIndex] != null)
        {
            return targetList[characterIndex];
        }

        // If no direct opposite, search adjacent slots
        int maxDistance = Mathf.Max(playerSlots.Count, enemySlots.Count);

        for (int distance = 1; distance < maxDistance; distance++)
        {
            // Create list of possible indices to check at this distance
            var possibleIndices = new List<int>();

            // Check above
            int upperIndex = characterIndex + distance;
            if (upperIndex >= 0 && upperIndex < targetList.Length && targetList[upperIndex] != null)
            {
                possibleIndices.Add(upperIndex);
            }

            // Check below
            int lowerIndex = characterIndex - distance;
            if (lowerIndex >= 0 && lowerIndex < targetList.Length && targetList[lowerIndex] != null)
            {
                possibleIndices.Add(lowerIndex);
            }

            // If we have any possible indices at this distance
            if (possibleIndices.Count > 0)
            {
                // Randomly choose one of the possible indices
                int randomIndex = possibleIndices[
                    UnityEngine.Random.Range(0, possibleIndices.Count)
                ];
                return targetList[randomIndex];
            }
        }

        return null;
    }

    public void ToggleThreatVisualization(bool visible)
    {
        if (threatVisualizer != null)
        {
            threatVisualizer.SetLineVisibility(visible);
        }
    }

    public void ClearThreatVisualization()
    {
        if (threatVisualizer != null)
        {
            threatVisualizer.ClearAllLines();
        }
    }

    public void SpawnEnemyWave(List<Character> enemyPrefabs)
    {
        if (isWaveInProgress)
        {
            Debug.LogWarning("Wave already in progress, cannot spawn new wave");
            return;
        }

        StartCoroutine(SpawnEnemyWaveCoroutine(enemyPrefabs));
    }

    private IEnumerator SpawnEnemyWaveCoroutine(List<Character> enemyPrefabs)
    {
        isWaveInProgress = true;

        // Clear existing enemies
        ClearEnemies();
        HideEnemyUnitFrames();

        // Spawn new enemies at WAVE_DISTANCE distance
        int enemyCount = Mathf.Min(enemyPrefabs.Count, enemySlots.Count);
        int enemyStartIndex = (enemySlots.Count - enemyCount) / 2;
        List<Character> spawnedEnemies = new List<Character>();

        for (int i = 0; i < enemyCount; i++)
        {
            if (!enemyPrefabs[i].IsEnemy)
            {
                Debug.LogError("Enemy character is player");
                continue;
            }

            var slotIndex = enemyStartIndex + i;
            var slotPosition = enemySlots[slotIndex].transform.position;
            var spawnPosition = slotPosition + Vector3.right * WAVE_DISTANCE;

            var character = Instantiate(enemyPrefabs[i], spawnPosition, Quaternion.identity);
            character.transform.localScale = new Vector3(-1, 1, 1);
            activeEnemyCharacters[slotIndex] = character;
            spawnedEnemies.Add(character);

            // Assign corresponding UnitFrameUI
            if (slotIndex < enemyFrames.Count)
            {
                characterToUnitFrame[character] = enemyFrames[slotIndex];
                InitializeUnitFrame(character, enemyFrames[slotIndex]);
            }
        }

        // Set all player characters to moving state
        SetAllCharactersState(CharacterState.Moving);

        // Move enemies and background to the left
        yield return StartCoroutine(MoveEnemiesToSlots(spawnedEnemies));

        // show enemy unit frames
        HideUnusedEnemyUnitFrames();

        // Set all player characters back to idle (start combat)
        SetAllCharactersState(CharacterState.Idle);

        // Move background back to original position
        background.position = originalBackgroundPosition;

        isWaveInProgress = false;
    }

    private void ClearEnemies()
    {
        for (int i = 0; i < activeEnemyCharacters.Length; i++)
        {
            if (activeEnemyCharacters[i] != null)
            {
                characterToUnitFrame.Remove(activeEnemyCharacters[i]);
                Destroy(activeEnemyCharacters[i].gameObject);
                activeEnemyCharacters[i] = null;
            }
        }
    }

    private void SetAllCharactersState(CharacterState state)
    {
        foreach (var character in activePlayerCharacters)
        {
            if (character != null)
            {
                character.SetState(state);
            }
        }

        foreach (var character in activeEnemyCharacters)
        {
            if (character != null)
            {
                character.SetState(state);
            }
        }
    }

    private IEnumerator MoveEnemiesToSlots(List<Character> enemies)
    {
        if (background == null)
            yield break;

        Vector3 backgroundStartPos = background.position;
        Vector3 backgroundTargetPos = backgroundStartPos + Vector3.left * WAVE_DISTANCE;

        float elapsedTime = 0f;
        float duration = WAVE_DISTANCE / waveMovementSpeed;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Move background
            background.position = Vector3.Lerp(backgroundStartPos, backgroundTargetPos, t);

            // Move enemies
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    var slotIndex = Array.IndexOf(activeEnemyCharacters, enemies[i]);
                    if (slotIndex != -1)
                    {
                        var slotPosition = enemySlots[slotIndex].transform.position;
                        var startPosition = slotPosition + Vector3.right * WAVE_DISTANCE;
                        enemies[i].transform.position = Vector3.Lerp(
                            startPosition,
                            slotPosition,
                            t
                        );
                    }
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final positions are exact
        background.position = backgroundTargetPos;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                var slotIndex = Array.IndexOf(activeEnemyCharacters, enemies[i]);
                if (slotIndex != -1)
                {
                    enemies[i].transform.position = enemySlots[slotIndex].transform.position;
                }
            }
        }
    }
}
