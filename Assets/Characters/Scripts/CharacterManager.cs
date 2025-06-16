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

    private List<CharacterSlot> playerSlots;
    private List<CharacterSlot> enemySlots;

    private Character[] activePlayerCharacters;
    private Character[] activeEnemyCharacters;

    private void Start()
    {
        var characterSlots = FindObjectsOfType<CharacterSlot>();
        playerSlots = new(Array.FindAll(characterSlots, (slot) => !slot.IsEnemy));
        enemySlots = new(Array.FindAll(characterSlots, (slot) => slot.IsEnemy));

        activePlayerCharacters = new Character[playerSlots.Count];
        activeEnemyCharacters = new Character[enemySlots.Count];

        // Sort slots by position from left to right
        playerSlots.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
        enemySlots.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

        InitializeBattle();
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
        }

        Debug.Log(
            $"Player characters: {string.Join(", ", new List<Character>(activePlayerCharacters).ConvertAll(c => c?.name ?? "null"))}"
        );
        Debug.Log(
            $"Enemy characters: {string.Join(", ", new List<Character>(activeEnemyCharacters).ConvertAll(c => c?.name ?? "null"))}"
        );
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
            }
        }

        Destroy(character.gameObject);
    }

    public Character GetOppositeCharacter(Character character)
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
}
