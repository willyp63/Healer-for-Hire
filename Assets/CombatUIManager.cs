using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatUIManager : MonoBehaviour
{
    [SerializeField]
    private List<UnitFrameUI> playerFrames;

    [SerializeField]
    private List<CardUI> cardUIs;

    [SerializeField]
    private ResourceBarUI resourceBarUI;

    [SerializeField]
    private List<Bounds> playerBounds;

    private int selectedFrameIndex = -1;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        // Initial card display update
        UpdateCardDisplays();

        // Initial resource bar update
        UpdateResourceBar();
    }

    private void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();

        // Update resource bar every frame to keep it current
        UpdateResourceBar();
    }

    private void HandleMouseInput()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(
            new Vector3(mousePosition.x, mousePosition.y, 0f)
        );
        worldMousePosition.z = 0f;

        // Get current hover index
        int hoveredIndex = GetFrameIndexAtPosition(worldMousePosition);

        // Update all frame highlight statuses
        for (int i = 0; i < playerFrames.Count; i++)
        {
            UnitFrameHighlightStatus status = UnitFrameHighlightStatus.None;

            if (i == selectedFrameIndex && i == hoveredIndex)
            {
                status = UnitFrameHighlightStatus.SelectedAndHovered;
            }
            else if (i == selectedFrameIndex)
            {
                status = UnitFrameHighlightStatus.Selected;
            }
            else if (i == hoveredIndex)
            {
                status = UnitFrameHighlightStatus.Hovered;
            }

            playerFrames[i].SetHighlightStatus(status);
        }

        // Handle clicks separately
        if (Input.GetMouseButtonDown(0))
        {
            int clickedIndex = GetFrameIndexAtPosition(worldMousePosition);
            if (clickedIndex >= 0 && clickedIndex < playerFrames.Count)
            {
                selectedFrameIndex = clickedIndex;
            }
        }
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            CycleFrameSelection(1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            CycleFrameSelection(-1);
        }

        // Handle card playing with 1, 2, 3, 4, 5, 6 keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayCard(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayCard(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayCard(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayCard(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            PlayCard(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            PlayCard(5);
        }
    }

    private void PlayCard(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= cardUIs.Count)
        {
            Debug.LogWarning($"Invalid card index: {cardIndex}");
            return;
        }

        List<Card> activeCards = DeckManager.Instance.ActiveCards;
        if (cardIndex >= activeCards.Count)
        {
            Debug.LogWarning($"No card at index {cardIndex}");
            return;
        }

        Card cardToPlay = activeCards[cardIndex];
        Debug.Log($"Playing card: {cardToPlay.cardName}");

        // Get target (hovering over a unit frame or if not hovering any unit, the selected unit)
        Character target = GetTarget();
        if (target == null)
        {
            Debug.LogWarning("No valid target found for card");
            return;
        }

        var mainPlayerCharacter = CharacterManager.Instance.MainPlayerCharacter;
        mainPlayerCharacter.AddResource(-cardToPlay.resourceCost);

        // Apply healing and status effect to the target
        if (cardToPlay.healAmount > 0)
        {
            target.Heal(cardToPlay.healAmount);
            Debug.Log($"Healed {target.name} for {cardToPlay.healAmount} health");
        }

        if (cardToPlay.statusEffectPrefab != null)
        {
            StatusEffect statusEffect = Instantiate(
                cardToPlay.statusEffectPrefab,
                target.transform
            );
            target.ApplyStatusEffect(statusEffect, mainPlayerCharacter);
            Debug.Log(
                $"Applied status effect {cardToPlay.statusEffectPrefab.EffectName} to {target.name}"
            );
        }

        // Update resource bar after playing card
        UpdateResourceBar();
    }

    private Character GetTarget()
    {
        // Get current mouse position in world coordinates
        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(
            new Vector3(mousePosition.x, mousePosition.y, 0f)
        );
        worldMousePosition.z = 0f;

        // Check if hovering over a unit frame
        int hoveredIndex = GetFrameIndexAtPosition(worldMousePosition);
        if (hoveredIndex >= 0 && hoveredIndex < playerFrames.Count)
        {
            // Get the character associated with this frame
            var playerCharacters = CharacterManager.Instance.GetActivePlayerCharacters();
            if (hoveredIndex < playerCharacters.Count && playerCharacters[hoveredIndex] != null)
            {
                return playerCharacters[hoveredIndex];
            }
        }

        // If not hovering over any unit frame, use the selected unit
        if (selectedFrameIndex >= 0 && selectedFrameIndex < playerFrames.Count)
        {
            var playerCharacters = CharacterManager.Instance.GetActivePlayerCharacters();
            if (
                selectedFrameIndex < playerCharacters.Count
                && playerCharacters[selectedFrameIndex] != null
            )
            {
                return playerCharacters[selectedFrameIndex];
            }
        }

        return null;
    }

    private void UpdateCardDisplays()
    {
        List<Card> activeCards = DeckManager.Instance.ActiveCards;

        // Update active cards
        for (int i = 0; i < cardUIs.Count; i++)
        {
            if (i < activeCards.Count)
            {
                cardUIs[i].SetCard(activeCards[i]);
            }
            else
            {
                // Set card UI to null if no card in this slot
                cardUIs[i].SetCard(null);
            }
        }
    }

    private void CycleFrameSelection(int direction)
    {
        if (playerFrames.Count == 0)
            return;

        if (selectedFrameIndex == -1)
        {
            // If no frame is selected, find the first active frame
            selectedFrameIndex = FindNextActiveFrameIndex(0, direction);
        }
        else
        {
            // Find the next active frame in the specified direction
            selectedFrameIndex = FindNextActiveFrameIndex(selectedFrameIndex, direction);
        }
    }

    private int FindNextActiveFrameIndex(int currentIndex, int direction)
    {
        int totalFrames = playerFrames.Count;
        int checkedFrames = 0;
        int index = currentIndex;

        // If no frame is currently selected, start from the beginning
        if (currentIndex == -1)
        {
            index = direction > 0 ? 0 : totalFrames - 1;
        }
        else
        {
            // Move to the next index in the specified direction
            index += direction;
        }

        // Loop through all frames to find an active one
        while (checkedFrames < totalFrames)
        {
            // Handle wrapping
            if (index < 0)
            {
                index = totalFrames - 1;
            }
            else if (index >= totalFrames)
            {
                index = 0;
            }

            // Check if this frame is active
            if (playerFrames[index] != null && playerFrames[index].gameObject.activeInHierarchy)
            {
                return index;
            }

            // Move to next frame in the specified direction
            index += direction;
            checkedFrames++;
        }

        // If no active frames found, return -1 (no selection)
        return -1;
    }

    private int GetFrameIndexAtPosition(Vector3 worldPosition)
    {
        for (int i = 0; i < playerBounds.Count; i++)
        {
            Bounds bounds = playerBounds[i];
            bool contains = bounds.Contains(worldPosition);
            if (contains)
            {
                return i;
            }
        }
        return -1;
    }

    public int GetSelectedFrameIndex()
    {
        return selectedFrameIndex;
    }

    public UnitFrameUI GetSelectedFrame()
    {
        if (selectedFrameIndex >= 0 && selectedFrameIndex < playerFrames.Count)
        {
            return playerFrames[selectedFrameIndex];
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerBounds == null)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < playerBounds.Count; i++)
        {
            Bounds bounds = playerBounds[i];

            // Draw wireframe cube for the bounds
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    private void UpdateResourceBar()
    {
        if (resourceBarUI == null)
            return;

        var mainPlayerCharacter = CharacterManager.Instance.MainPlayerCharacter;
        if (mainPlayerCharacter == null)
            return;

        resourceBarUI.SetResource(
            mainPlayerCharacter.CurrentResource,
            mainPlayerCharacter.MaxResource
        );
    }
}
