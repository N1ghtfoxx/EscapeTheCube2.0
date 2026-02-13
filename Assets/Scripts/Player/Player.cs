using UnityEngine;

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
            // TODO: implement no hyration loss?
            Debug.Log($"{GetPlayerName()} moved without hydration loss (Power Outage active).");
        }

        // Reset mandatory turn flag after movement (Energy Drink effect)
        if (nextTurnMandatory)
        {
            nextTurnMandatory = false;
            Debug.Log($"{GetPlayerName()} completed mandatory turn (Energy Drink effect ended).");
        }

        currentField = newField;
        transform.position = newField.transform.position;

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
    /// Moves player to the field's position
    /// </summary>
    public void SetCurrentField(Field field)
    {
        currentField = field;

        // Move player to the field's position
        if (field != null)
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
    /// Teleports player to adjacent Theke (counter) field if available
    /// </summary>
    public void ActivateSecretPassage()
    {
        Field targetTheke = null;
        foreach (Field neighbour in currentField.GetNeighbours())
        {
            if (neighbour is Theke1)
            {
                targetTheke = neighbour;
                break;
            }
        }

        if (targetTheke != null)
        {
            MoveToField(targetTheke);
            Debug.Log($"{GetPlayerName()} teleported to adjacent Theke via secret passage.");
        }
        else
        {
            Debug.Log($"{GetPlayerName()} no adjacent Theke found for secret passage.");
        }
    }

    #endregion
}