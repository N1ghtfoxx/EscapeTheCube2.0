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

    #endregion

    #region Movement

    /// <summary>
    /// Moves player to a new field
    /// Handles hydration loss (unless Power Outage is active)
    /// Resets Energy Drink mandatory turn flag if active
    /// Triggers field arrival events and player switching
    /// </summary>
    public void MoveToField(Field newField)
    {
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

        // Reset mandatory turn flag after movement (Energy Drink effect)
        if (nextTurnMandatory)
        {
            nextTurnMandatory = false;
            Debug.Log($"{GetPlayerName()} completed mandatory turn (Energy Drink effect ended).");
        }

        currentField = newField;

        // Calculate position with offset if multiple players on same field
        Vector3 targetPosition = newField.transform.position;

        // Get all players on this field
        List<Player> playersOnField = GameManager.Instance.GetAllPlayers()
            .Where(p => p.GetCurrentField() == newField && p != this)
            .ToList();

        if (playersOnField.Count > 0)
        {
            // There are other players here - add offset in a circle
            int totalPlayers = playersOnField.Count + 1; // +1 for this player
            int myIndex = playersOnField.Count; // This player is the newest arrival

            float angle = (360f / totalPlayers) * myIndex;
            float radius = 0.15f; // Kleinerer Abstand - bleiben auf dem Field
            float offsetX = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float offsetY = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

            targetPosition += new Vector3(offsetX, offsetY, 0);
        }

        transform.position = targetPosition;

        // Notify the field that player arrived
        newField.OnPlayerArrived(this);

        // Switch to the next player
        GameManager.Instance.NextPlayer();
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
        currentField = field;

        // Move player to the field's position (unless explicitly disabled)
        if (field != null && moveToPosition)
        {
            transform.position = field.transform.position;
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
        // Empty by default - can be extended by other systems
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
    /// IMPORTANT: This is drawn as a card AFTER the turn, so it ends the turn and calls NextPlayer()
    /// Flow: Player moves → draws card → Secret Passage activates → teleports to nearest Theke → turn ends
    /// </summary>
    public void ActivateSecretPassage()
    {
        Field targetTheke = FindNearestTheke();

        if (targetTheke != null)
        {
            // use MoveToField() to handle all movement logic including NextPlayer()
            MoveToField(targetTheke);
            Debug.Log($"{GetPlayerName()} teleported to nearest Theke via secret passage.");
        }
        else
        {
            Debug.Log($"{GetPlayerName()} - no Theke found in game. Card effect wasted.");
            // even if no theke found, the turn still ends (card was drawn)
            GameManager.Instance.NextPlayer();
        }
    }

    /// <summary>
    /// Finds the nearest Theke field in the entire game
    /// Returns null if no Theke exists
    /// Uses ITheke interface to find both Theke1 and Theke2 fields
    /// </summary>
    private Field FindNearestTheke()
    {
        // get all fields in teh game via GameManager
        List<Field> allFields = FindObjectsByType<Field>(FindObjectsSortMode.None).ToList();

        Field nearestTheke = null;
        float shortestDistance = float.MaxValue;

        foreach (Field field in allFields)
        {
            // check if this fiel is a theke
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