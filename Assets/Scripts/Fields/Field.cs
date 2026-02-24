// made by Naomi in collaboration with Claude Ai

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Base class for all board fields in the game
/// Handles click detection, neighbour management, and player arrival callbacks
/// Inherit from this class and override OnFieldClicked() / OnPlayerArrived() for custom field behaviour
/// </summary>
public class Field : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    // Neighbouring fields the current player can move to from this field
    [SerializeField] protected List<Field> neighbours = new List<Field>();

    [Header("Player Layout")]
    [Tooltip("When enabled, players standing on this field are arranged VERTICALLY")]
    [SerializeField] private bool verticalPlayerLayout = false;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    // Whether this field currently accepts mouse clicks
    protected bool isClickable = false;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    protected virtual void Update()
    {
        // Poll for a left-mouse-button press each frame
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckIfClicked();
        }
    }

    // -------------------------------------------------------------------------
    // Click Detection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tests whether the current mouse position overlaps this field's collider
    /// Fires OnFieldClicked() if the field is clickable and the cursor is over it
    /// </summary>
    private void CheckIfClicked()
    {
        if (!isClickable) return;

        // Convert screen-space mouse position to world space
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // Test against the field's 2D collider
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition);

        if (hitCollider != null && hitCollider.gameObject == gameObject)
        {
            OnFieldClicked();
        }
    }

    // -------------------------------------------------------------------------
    // Overrideable Callbacks
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when the player clicks this field
    /// Default behaviour: moves the current player here
    /// Override in subclasses to add entry conditions (e.g. Exit, Bistro)
    /// </summary>
    protected virtual void OnFieldClicked()
    {
        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();
        currentPlayer.MoveToField(this);
    }

    /// <summary>
    /// Called when a player finishes moving onto this field
    /// Override in subclasses to apply field-specific effects
    /// </summary>
    public virtual void OnPlayerArrived(Player player)
    {
        // No default behaviour — child classes override as needed
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    // Enables or disables click detection for this field
    public void SetClickable(bool clickable)
    {
        isClickable = clickable;
    }

    // Returns all fields directly reachable from this one
    public List<Field> GetNeighbours()
    {
        return neighbours;
    }

    /// <summary>
    /// Registers a new neighbour if it is not already in the list
    /// </summary>
    public void AddNeighbour(Field neighbour)
    {
        if (!neighbours.Contains(neighbour))
        {
            neighbours.Add(neighbour);
        }
    }

    /// <summary>
    /// Returns true if players on this field should be stacked vertically
    /// Checked by Player.RepositionPlayersOnField() during layout
    /// </summary>
    public bool IsVerticalLayout() => verticalPlayerLayout;
}