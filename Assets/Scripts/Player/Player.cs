using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Player : MonoBehaviour
{
    #region Serialized Fields

    [SerializeField] private string playerName;
    [SerializeField] private int hydration = 10;
    [SerializeField] private int maxHydration = 10;

    #endregion

    #region Private Fields

    private Field currentField;

    [Header("Card Inventory")]
    private int hintCards = 0;
    private int accessCards = 0;

    [Header("Special States")]
    private bool nextTurnMandatory = false; // Energy Drink effect
    private bool hasVisitedWC = false;

    [Header("Player Layout Settings")]
    [Tooltip("Abstand zwischen Spielern auf demselben Feld")]
    [SerializeField] private float playerSpacing = 0.22f;

    #endregion

    #region Movement

    /// <summary>
    /// Moves player to a new field.
    /// Handles hydration loss (unless Power Outage is active).
    /// Resets Energy Drink mandatory turn flag if active.
    /// Triggers field arrival events.
    /// Repositions ALL players on the target field in a clean row.
    /// NOTE: Does NOT end the turn! Player can still draw cards after moving.
    /// Turn ends when player draws a card via CardDeckButtonHandler -> GameManager.OnCardDrawn()
    /// </summary>
    public void MoveToField(Field newField)
    {
        // CRITICAL: Disable all fields IMMEDIATELY to prevent multiple moves
        // This must be first to prevent the player from clicking another field
        GameManager.Instance.DisableAllFields();

        // FIX:
        // Reset mandatory turn flag BEFORE SetPlayerMoved()
        // so UpdateCardButtons() correctly reads nextTurnMandatory = false
        if (nextTurnMandatory)
        {
            nextTurnMandatory = false;
            Debug.Log($"{GetPlayerName()} completed mandatory turn (Energy Drink effect ended).");
        }

        // Signal that player moved this turn
        GameManager.Instance.SetPlayerMoved(true);

        // Check if power outage is active (global effect)
        bool isPowerOutage = GameManager.Instance.IsPowerOutageActive();

        if (!isPowerOutage)
        {
            // Normal hydration loss
            ChangeHydration(-1);
            Debug.Log($"{GetPlayerName()} moved and lost 1 hydration. Current: {GetHydration()}/{GetMaxHydration()}");
        }
        else
        {
            // Power outage active - no hydration loss
            Debug.Log($"{GetPlayerName()} moved without hydration loss (Power Outage active).");
        }

        // Update currentField and reposition everyone on both the old and new field
        Field oldField = currentField;
        currentField = newField;

        // Reposition players on the OLD field (one fewer player now)
        if (oldField != null)
            RepositionPlayersOnField(oldField);

        // Reposition players on the NEW field (includes this player)
        RepositionPlayersOnField(newField);

        // Notify the field that player arrived
        newField.OnPlayerArrived(this);

        // DO NOT call NextPlayer() here!
        // Turn ends when player draws a card (CardDeckButtonHandler -> GameManager.OnCardDrawn)
    }

    /// <summary>
    /// Repositions all players currently on the given field in a clean, centered row.
    /// Horizontal by default — vertical if the field has IsVerticalLayout() == true (e.g. Terrasse).
    /// Called whenever a player arrives at or leaves a field so the layout stays clean.
    ///
    /// SAFE during initialization: if GameManager isn't ready yet, only this player
    /// is positioned at the field center (correct for single-player spawn scenarios).
    /// </summary>
    private void RepositionPlayersOnField(Field field)
    {
        if (field == null) return;

        // Safe fallback during initialization: GameManager not ready yet
        // → just place this player exactly at the field center and return
        if (GameManager.Instance == null)
        {
            transform.position = field.transform.position;
            Debug.Log($"[Player] RepositionPlayersOnField: GameManager not ready, placing {GetPlayerName()} at field center.");
            return;
        }

        // Gather all players on this field
        List<Player> allPlayers = GameManager.Instance.GetAllPlayers();
        if (allPlayers == null || allPlayers.Count == 0)
        {
            // GameManager exists but hasn't registered players yet (e.g. mid-spawn)
            transform.position = field.transform.position;
            Debug.Log($"[Player] RepositionPlayersOnField: No players registered yet, placing {GetPlayerName()} at field center.");
            return;
        }

        List<Player> playersOnField = allPlayers
            .Where(p => p != null && p.GetCurrentField() == field)
            .ToList();

        int count = playersOnField.Count;
        if (count == 0) return;

        bool vertical = field.IsVerticalLayout();
        Vector3 center = field.transform.position;

        // Centered layout: offset starts at -(totalSpan / 2) so the group is
        // symmetrically centered on the field's transform position
        float totalSpan = (count - 1) * playerSpacing;
        float startOffset = -totalSpan / 2f;

        for (int i = 0; i < count; i++)
        {
            float offset = startOffset + i * playerSpacing;
            Vector3 pos = center;

            if (vertical)
                pos.y += offset;  // Terrasse: stack top-to-bottom
            else
                pos.x += offset;  // Default: stack left-to-right

            playersOnField[i].transform.position = pos;

            // Give each player a unique sortingOrder so nobody disappears
            // behind another. Higher index = rendered on top.
            SpriteRenderer sr = playersOnField[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = i;
        }

        Debug.Log($"[Player] Repositioned {count} player(s) on field '{field.name}' ({(vertical ? "vertical" : "horizontal")}).");
    }

    /// <summary>
    /// Returns the field the player is currently on
    /// </summary>
    public Field GetCurrentField()
    {
        return currentField;
    }

    /// <summary>
    /// Sets the player's starting field (called during initialization)
    /// Optionally moves player to the field's position
    /// NOTE: Does NOT trigger OnPlayerArrived() or NextPlayer() - use for silent teleports/initialization only
    /// For normal movement, use MoveToField() instead
    /// </summary>
    /// <param name="field">The field to assign</param>
    /// <param name="moveToPosition">If true, moves player to field position. If false, keeps current position.</param>
    public void SetCurrentField(Field field, bool moveToPosition = true)
    {
        Field oldField = currentField;
        currentField = field;

        // Move player to the field's position (unless explicitly disabled)
        if (field != null && moveToPosition)
        {
            transform.position = field.transform.position;

            // Reposition everyone on the old field (one fewer player now)
            if (oldField != null)
                RepositionPlayersOnField(oldField);

            // Reposition everyone on the new field
            RepositionPlayersOnField(field);
        }
    }

    #endregion

    #region Hydration Management

    /// <summary>
    /// Changes the player's hydration level
    /// Automatically clamps between 0 and maxHydration
    /// </summary>
    public void ChangeHydration(int amount)
    {
        hydration = Mathf.Clamp(hydration + amount, 0, maxHydration);
        OnHydrationChanged();

        // automatically update UI
        if (UiManager.Instance != null)
        {
            UiManager.Instance.UpdatePlayerUI(this);
        }
    }

    /// <summary>
    /// Returns current hydration value
    /// </summary>
    public int GetHydration()
    {
        return hydration;
    }

    /// <summary>
    /// Returns max hydration value
    /// </summary>
    public int GetMaxHydration()
    {
        return maxHydration;
    }

    /// <summary>
    /// Called when hydration changes
    /// Can be used for UI updates or other systems
    /// </summary>
    protected virtual void OnHydrationChanged()
    {
        if (hydration <= 0)
        {
            Debug.Log($"{GetPlayerName()} has no hydration left - eliminated!");
            GameManager.Instance.EliminatePlayer(this);
        }
    }

    #endregion

    #region Player Info

    /// <summary>
    /// Returns the player's name
    /// </summary>
    public string GetPlayerName()
    {
        return playerName;
    }

    /// <summary>
    /// Sets the player's name (called by GameManager during spawn)
    /// </summary>
    public void SetPlayerName(string name)
    {
        playerName = name;
        Debug.Log($"Player name set to: {playerName}");
    }

    #endregion

    #region Hint Card Management

    /// <summary>
    /// Returns current number of hint cards
    /// </summary>
    public int GetHintCards()
    {
        return hintCards;
    }

    /// <summary>
    /// Adds hint cards to player's inventory (called by CardManager)
    /// </summary>
    public void AddHintCard(int amount = 1)
    {
        hintCards += amount;
        Debug.Log($"{GetPlayerName()} gained {amount} hint card(s). Total: {hintCards}");

        if (UiManager.Instance != null)
        {
            UiManager.Instance.UpdatePlayerUI(this);
        }

    }

    /// <summary>
    /// Removes hint cards from player's inventory (called by CardManager)
    /// </summary>
    public void RemoveHintCard(int amount = 1)
    {
        hintCards = Mathf.Max(0, hintCards - amount);
        Debug.Log($"{GetPlayerName()} lost {amount} hint card(s). Total: {hintCards}");

        if (UiManager.Instance != null)
        {
            UiManager.Instance.UpdatePlayerUI(this);
        }

    }

    #endregion

    #region Access Card Management

    /// <summary>
    /// Returns current number of access cards
    /// </summary>
    public int GetAccessCards()
    {
        return accessCards;
    }

    /// <summary>
    /// Checks if player has at least one access card
    /// </summary>
    public bool HasAccessCard()
    {
        return accessCards > 0;
    }

    /// <summary>
    /// Adds access cards to player's inventory (called by CardManager)
    /// </summary>
    public void AddAccessCard(int amount = 1)
    {
        accessCards += amount;
        Debug.Log($"{GetPlayerName()} gained {amount} access card(s). Total: {accessCards}");

        if (UiManager.Instance != null)
        {
            UiManager.Instance.UpdatePlayerUI(this);
        }

    }

    /// <summary>
    /// Consumes access cards from player's inventory (called by Bistro.cs)
    /// </summary>
    public void ConsumeAccessCard(int amount = 1)
    {
        accessCards = Mathf.Max(0, accessCards - amount);
        Debug.Log($"{GetPlayerName()} consumed {amount} access card(s). Total: {accessCards}");

        if (UiManager.Instance != null)
        {
            UiManager.Instance.UpdatePlayerUI(this);
        }
    }

    #endregion

    #region WC and Win Conditions

    /// <summary>
    /// Checks if player can enter WC (requires 3 hint cards)
    /// </summary>
    public bool CanEnterWC()
    {
        return hintCards >= 3;
    }

    /// <summary>
    /// Checks if player has visited the WC
    /// </summary>
    public bool HasVisitedWC()
    {
        return hasVisitedWC;
    }

    /// <summary>
    /// Sets WC visit status (called by WC.cs)
    /// </summary>
    public void SetHasVisitedWC(bool value)
    {
        hasVisitedWC = value;
    }

    /// <summary>
    /// Checks if player meets win conditions (3 hint cards + WC visit)
    /// </summary>
    public bool CanWin()
    {
        return hintCards >= 3 && hasVisitedWC;
    }

    #endregion

    #region Card Effect - Energy Drink (Mandatory Turn)

    /// <summary>
    /// Sets whether next turn is mandatory (called by CardManager for Energy Drink effect)
    /// When true, player cannot skip movement - must move before drawing cards
    /// </summary>
    public void SetNextTurnMandatory(bool value)
    {
        nextTurnMandatory = value;
        if (value)
        {
            Debug.Log($"{GetPlayerName()}'s next turn is mandatory (Energy Drink effect).");
        }
    }

    /// <summary>
    /// Checks if next turn is mandatory (called by GameManager)
    /// </summary>
    public bool IsNextTurnMandatory()
    {
        return nextTurnMandatory;
    }

    #endregion

    #region Card Effect - Secret Passage

    /// <summary>
    /// Activates secret passage effect (called by CardManager)
    /// Teleports player to the nearest Theke (counter) field in the entire game
    /// IMPORTANT: This is drawn as a card AFTER a turn
    /// Flow: Player moves → draws Item card → Secret Passage activates → teleports to nearest Theke → turn ends
    /// NOTE: Does NOT call NextPlayer() - that's handled by the card button handler
    /// NOTE: Does NOT set playerMovedThisTurn flag - this is a teleport from a card effect, not a normal move
    /// </summary>
    public void ActivateSecretPassage()
    {
        Field targetTheke = FindNearestTheke();

        if (targetTheke != null)
        {
            // Teleport to the nearest Theke
            // Use SetCurrentField to avoid triggering movement logic
            SetCurrentField(targetTheke, moveToPosition: true);

            // Trigger the field's arrival event (for special field effects)
            targetTheke.OnPlayerArrived(this);

            // Update clickable fields but DON'T set playerMovedThisTurn flag
            // The player should NOT be able to move again after this teleport
            GameManager.Instance.UpdateClickableFields();

            Debug.Log($"{GetPlayerName()} teleported to nearest Theke via secret passage. Turn ends.");
        }
        else
        {
            Debug.Log($"{GetPlayerName()} - no Theke found in game. Card effect wasted.");
        }

        // Turn ending is handled by CardDeckButtonHandler -> GameManager.OnCardDrawn()
    }

    /// <summary>
    /// Finds the nearest Theke field in the entire game
    /// Returns null if no Theke exists
    /// Uses ITheke interface to find both Theke1 and Theke2 fields
    /// </summary>
    private Field FindNearestTheke()
    {
        // get all fields in the game via GameManager
        List<Field> allFields = FindObjectsByType<Field>(FindObjectsSortMode.None).ToList();

        Field nearestTheke = null;
        float shortestDistance = float.MaxValue;

        foreach (Field field in allFields)
        {
            // check if this field is a theke
            if (field is ITheke)
            {
                // calculate distance from player's current position to this field
                float distance = Vector3.Distance(transform.position, field.transform.position);

                // update nearest if this one is closer
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