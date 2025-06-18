using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterManager : Singleton<CharacterManager>
{
    [SerializeField]
    private List<Character> playerCharacters = new();

    [SerializeField]
    private List<Character> enemyCharacters = new();

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

    private Character[] activePlayerCharacters;
    private Character[] activeEnemyCharacters;

    // Mapping between characters and their UnitFrameUI
    private Dictionary<Character, UnitFrameUI> characterToUnitFrame = new();

    // Update interval for UI
    private const float UI_UPDATE_INTERVAL = 0.1f;

    private void Start()
    {
        activePlayerCharacters = new Character[playerSlots.Count];
        activeEnemyCharacters = new Character[enemySlots.Count];

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
        }

        // Spawn enemy characters
        int enemyCount = Mathf.Min(enemyCharacters.Count, enemySlots.Count);
        int enemyStartIndex = (enemySlots.Count - enemyCount) / 2;

        for (int i = 0; i < enemyCount; i++)
        {
            if (!enemyCharacters[i].IsEnemy)
                Debug.LogError("Enemy character is player");

            var slotIndex = enemyStartIndex + i;
            var character = Instantiate(
                enemyCharacters[i],
                enemySlots[slotIndex].transform.position,
                Quaternion.identity
            );
            character.transform.localScale = new(-1, 1, 1);
            activeEnemyCharacters[slotIndex] = character;

            // Assign corresponding UnitFrameUI
            if (slotIndex < enemyFrames.Count)
            {
                characterToUnitFrame[character] = enemyFrames[slotIndex];
                InitializeUnitFrame(character, enemyFrames[slotIndex]);
            }
        }

        // Hide unused UnitFrames
        HideUnusedUnitFrames();
    }

    private void HideUnusedUnitFrames()
    {
        // Hide unused player UnitFrames
        for (int i = 0; i < playerFrames.Count; i++)
        {
            if (activePlayerCharacters[i] == null)
            {
                playerFrames[i].gameObject.SetActive(false);
            }
        }

        // Hide unused enemy UnitFrames
        for (int i = 0; i < enemyFrames.Count; i++)
        {
            if (activeEnemyCharacters[i] == null)
            {
                enemyFrames[i].gameObject.SetActive(false);
            }
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
}
