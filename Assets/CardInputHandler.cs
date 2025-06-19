using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInputHandler : MonoBehaviour
{
    [SerializeField]
    private List<UnitFrameUI> playerFrames;

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
    }

    private void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
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
}
