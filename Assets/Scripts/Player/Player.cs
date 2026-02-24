// made by Naomi in collaboration with Claude Ai

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents one player token on the board
///
/// Responsibilities:
///   - Moving between fields and triggering arrival events
///   - Tracking and clamping hydration (elimination at 0)
///   - Managing the card inventory (hint cards, access cards)
///   - Holding special-state flags (Energy Drink, WC visit)
///   - Repositioning all tokens on a field in a neat, centred row
/// </summary>
public class Player : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [SerializeField] private string playerName;
    [SerializeField] private int hydration = 10;
    [SerializeField] private int maxHydration = 10;

    // -------------------------------------------------------------------------
    // Private Fields
    // -------------------------------------------------------------------------

    private Field currentField;

    // Card inventory
    [Header("Card Inventory")]
    private int hintCards = 0;
    private int accessCards = 0;

    // Special-state flags
    [Header("Special States")]
    private bool nextTurnMandatory = false; // True while Energy Drink effect is active
    private bool hasVisitedWC = false;

    // Token layout
    [Header("Player Layout Settings")]
    [Tooltip("Spacing (in world units) between player tokens standing on the same field.")]
    [SerializeField] private float playerSpacing = 0.22f;

    #region Movement

    /// <summary>
    /// Moves the player to a new field
    ///
    /// What this method does:
    ///   1. Immediately disables all fields to prevent a second click
    ///   2. Clears the Energy Drink mandatory-move flag if active
    ///   3. Marks the player as having moved this turn
    ///   4. Deducts 1 hydration (unless Power Outage is active)
    ///   5. Repositions all tokens on both the old and new field
    ///   6. Calls OnPlayerArrived() on the destination field
    ///
    /// What this method does NOT do:
    ///   - Call NextPlayer(). The turn ends when the player draws a card
    ///     (CardDeckButtonHandler → GameManager.OnCardDrawn()).
    /// </summary>
    public void MoveToField(Field newField)
    {
        // Disable all fields immediately to block additional moves
        GameManager.Instance.DisableAllFields();

        // Clear the mandatory-move flag BEFORE SetPlayerMoved() so that
        // UpdateCardButtons() reads the correct (false) value on its next call
        if (nextTurnMandatory)
        {
            nextTurnMandatory = false;
            Debug.Log($"{GetPlayerName()} completed mandatory turn (Energy Drink effect ended).");
        }

        // Notify GameManager that the player has moved
        GameManager.Instance.SetPlayerMoved(true);

        // Apply hydration loss (skipped during Power Outage)
        bool isPowerOutage = GameManager.Instance.IsPowerOutageActive();
        if (!isPowerOutage)
        {
            ChangeHydration(-1);
            Debug.Log($"{GetPlayerName()} moved and lost 1 hydration. " +
                      $"Current: {GetHydration()}/{GetMaxHydration()}");
        }
        else
        {
            Debug.Log($"{GetPlayerName()} moved without hydration loss (Power Outage active).");
        }

        // Update position tracking and reposition all tokens on both affected fields
        Field oldField = currentField;
        currentField = newField;

        if (oldField != null)
            RepositionPlayersOnField(oldField);   // One fewer player on the old field

        RepositionPlayersOnField(newField);        // One more player on the new field

        // Trigger field-specific arrival logic
        newField.OnPlayerArrived(this);
    }

    /// <summary>
    /// Repositions all tokens standing on <paramref name="field"/> in a centred row
    ///
    /// Layout axis:
    ///   - Horizontal (X) by default.
    ///   - Vertical (Y) when <see cref="Field.IsVerticalLayout"/> returns true (e.g. Terrasse)
    ///
    /// Safe during initialisation: falls back to placing only this token at the
    /// field centre when GameManager is not yet available
    /// </summary>
    private void RepositionPlayersOnField(Field field)
    {
        if (field == null) return;

        // Fallback: GameManager not ready yet (mid-spawn)
        if (GameManager.Instance == null)
        {
            transform.position = field.transform.position;
            Debug.Log($"[Player] RepositionPlayersOnField: GameManager not ready, " +
                      $"placing {GetPlayerName()} at field centre.");
            return;
        }

        List<Player> allPlayers = GameManager.Instance.GetAllPlayers();
        if (allPlayers == null || allPlayers.Count == 0)
        {
            // Players haven't been registered yet (e.g. mid-spawn loop)
            transform.position = field.transform.position;
            Debug.Log($"[Player] RepositionPlayersOnField: No players registered yet, " +
                      $"placing {GetPlayerName()} at field centre.");
            return;
        }

        // Collect only the players currently on this field
        List<Player> playersOnField = allPlayers
            .Where(p => p != null && p.GetCurrentField() == field)
            .ToList();

        int count = playersOnField.Count;
        if (count == 0) return;

        bool vertical = field.IsVerticalLayout();
        Vector3 center = field.transform.position;

        // Symmetrically centre the group around the field's transform position
        float totalSpan = (count - 1) * playerSpacing;
        float startOffset = -totalSpan / 2f;

        for (int i = 0; i < count; i++)
        {
            float offset = startOffset + i * playerSpacing;
            Vector3 pos = center;

            if (vertical)
                pos.y += offset;  // Stack top-to-bottom for vertical fields (e.g. Terrasse)
            else
                pos.x += offset;  // Stack left-to-right for all other fields

            playersOnField[i].transform.position = pos;

            // Assign a unique sorting order so tokens never hide behind each other
            SpriteRenderer sr = playersOnField[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = i;
        }

        Debug.Log($"[Player] Repositioned {count} token(s) on '{field.name}' " +
                  $"({(vertical ? "vertical" : "horizontal")}).");
    }

    // Returns the field the player is currently standing on
    public Field GetCurrentField() => currentField;

    /// <summary>
    /// Silently teleports the player to a field without triggering normal movement logic
    ///
    /// Used for:
    ///   - Initial placement during game setup
    ///   - Card-effect teleports (e.g. Secret Passage)
    ///
    /// Does NOT trigger OnPlayerArrived(), hydration loss, or NextPlayer()
    /// For normal movement use MoveToField() instead
    /// </summary>
    /// <param name="field">Destination field</param>
    /// <param name="moveToPosition">When true, physically moves the token to the field position</param>
    public void SetCurrentField(Field field, bool moveToPosition = true)
    {
        Field oldField = currentField;
        currentField = field;

        if (field != null && moveToPosition)
        {
            transform.position = field.transform.position;

            if (oldField != null)
                RepositionPlayersOnField(oldField);

            RepositionPlayersOnField(field);
        }
    }

    #endregion

    #region Hydration Management

    /// <summary>
    /// Changes hydration by <paramref name="amount"/> (positive = gain, negative = loss)
    /// Automatically clamps to [0, maxHydration] and triggers elimination at 0
    /// Refreshes the player's HUD panel automatically
    /// </summary>
    public void ChangeHydration(int amount)
    {
        hydration = Mathf.Clamp(hydration + amount, 0, maxHydration);
        OnHydrationChanged();

        if (UiManager.Instance != null)
            UiManager.Instance.UpdatePlayerUI(this);
    }

    // Returns current hydration
    public int GetHydration() => hydration;

    // Returns maximum hydration
    public int GetMaxHydration() => maxHydration;

    /// <summary>
    /// Called after every hydration change
    /// Eliminates the player if hydration reaches zero
    /// </summary>
    protected virtual void OnHydrationChanged()
    {
        if (hydration <= 0)
        {
            Debug.Log($"{GetPlayerName()} has no hydration left – eliminated!");
            GameManager.Instance.EliminatePlayer(this);
        }
    }

    #endregion

    #region Player Info

    // Returns the player's display name
    public string GetPlayerName() => playerName;

    /// <summary>
    /// Sets the player's display name
    /// Called by GameManager during player spawning
    /// </summary>
    public void SetPlayerName(string name)
    {
        playerName = name;
        Debug.Log($"Player name set to: {playerName}");
    }

    #endregion

    #region Hint Card Management

    // Returns the current number of hint cards
    public int GetHintCards() => hintCards;

    /// <summary>
    /// Adds hint cards to the inventory and refreshes the HUD
    /// Called by CardManager and field scripts (e.g. Bistro)
    /// </summary>
    public void AddHintCard(int amount = 1)
    {
        hintCards += amount;
        Debug.Log($"{GetPlayerName()} gained {amount} hint card(s). Total: {hintCards}");

        if (UiManager.Instance != null)
            UiManager.Instance.UpdatePlayerUI(this);
    }

    /// <summary>
    /// Removes hint cards from the inventory (clamped at 0) and refreshes the HUD
    /// Called by CardManager
    /// </summary>
    public void RemoveHintCard(int amount = 1)
    {
        hintCards = Mathf.Max(0, hintCards - amount);
        Debug.Log($"{GetPlayerName()} lost {amount} hint card(s). Total: {hintCards}");

        if (UiManager.Instance != null)
            UiManager.Instance.UpdatePlayerUI(this);
    }

    #endregion

    #region Access Card Management

    // Returns the current number of access cards
    public int GetAccessCards() => accessCards;

    // Returns true if the player holds at least one access card
    public bool HasAccessCard() => accessCards > 0;

    /// <summary>
    /// Adds access cards to the inventory and refreshes the HUD
    /// Called by CardManager
    /// </summary>
    public void AddAccessCard(int amount = 1)
    {
        accessCards += amount;
        Debug.Log($"{GetPlayerName()} gained {amount} access card(s). Total: {accessCards}");

        if (UiManager.Instance != null)
            UiManager.Instance.UpdatePlayerUI(this);
    }

    /// <summary>
    /// Consumes access cards (clamped at 0) and refreshes the HUD
    /// Called by Bistro.cs on field arrival
    /// </summary>
    public void ConsumeAccessCard(int amount = 1)
    {
        accessCards = Mathf.Max(0, accessCards - amount);
        Debug.Log($"{GetPlayerName()} consumed {amount} access card(s). Total: {accessCards}");

        if (UiManager.Instance != null)
            UiManager.Instance.UpdatePlayerUI(this);
    }

    #endregion

    #region WC and Win Conditions

    // Returns true if the player has the 3 hint cards required to use the WC
    public bool CanEnterWC() => hintCards >= 3;

    // Returns true if the player has already visited the WC this game
    public bool HasVisitedWC() => hasVisitedWC;

    /// <summary>
    /// Marks the WC as visited (or unvisited)
    /// Called by WC.cs on arrival when the player meets the entry requirement
    /// </summary>
    public void SetHasVisitedWC(bool value)
    {
        hasVisitedWC = value;
    }

    /// <summary>
    /// Returns true when all win conditions are met: 3 hint cards AND WC visited
    /// Used by Exit.cs to allow or block entry
    /// </summary>
    public bool CanWin() => hintCards >= 3 && hasVisitedWC;

    #endregion

    #region Card Effect — Energy Drink (Mandatory Turn)

    /// <summary>
    /// Enables or disables the mandatory-move flag (Energy Drink card effect)
    /// While true, the player cannot draw cards — they must move first
    /// The flag is cleared automatically at the start of MoveToField()
    /// </summary>
    public void SetNextTurnMandatory(bool value)
    {
        nextTurnMandatory = value;
        if (value)
            Debug.Log($"{GetPlayerName()}'s next turn is mandatory (Energy Drink effect).");
    }

    /// <summary>
    /// Returns true while the Energy Drink mandatory-move effect is active
    /// Queried by GameManager.CanCurrentPlayerSkipMovement()
    /// </summary>
    public bool IsNextTurnMandatory() => nextTurnMandatory;

    #endregion

    #region Card Effect — Secret Passage

    /// <summary>
    /// Teleports the player to the nearest Theke (counter) field on the board
    ///
    /// Card draw flow:
    ///   Player moves → draws Item card → Secret Passage activates
    ///   → teleports to nearest Theke → turn ends (via CardDeckButtonHandler)
    ///
    /// Notes:
    ///   - Does NOT set playerMovedThisTurn (card-effect teleport ≠ normal move)
    ///   - Does NOT call NextPlayer() — handled by CardDeckButtonHandler
    /// </summary>
    public void ActivateSecretPassage()
    {
        Field targetTheke = FindNearestTheke();

        if (targetTheke != null)
        {
            // Silently teleport (no hydration loss, no movement flag)
            SetCurrentField(targetTheke, moveToPosition: true);

            // Run the field's arrival logic (e.g. Theke1 safe-zone message)
            targetTheke.OnPlayerArrived(this);

            // Re-evaluate which fields are clickable (player cannot move again)
            GameManager.Instance.UpdateClickableFields();

            Debug.Log($"{GetPlayerName()} teleported to nearest Theke via Secret Passage. Turn ends.");
        }
        else
        {
            Debug.Log($"{GetPlayerName()} – no Theke found in the scene. Secret Passage had no effect.");
        }
    }

    /// <summary>
    /// Finds the closest ITheke field on the board by straight-line distance
    /// Returns null if no Theke exists in the scene
    /// </summary>
    private Field FindNearestTheke()
    {
        Field nearestTheke = null;
        float shortestDistance = float.MaxValue;

        // Search all Field components in the scene for one that also implements ITheke
        foreach (Field field in FindObjectsByType<Field>(FindObjectsSortMode.None))
        {
            if (field is ITheke)
            {
                float distance = Vector3.Distance(transform.position, field.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestTheke = field;
                }
            }
        }

        return nearestTheke;
    }

    #endregion
}