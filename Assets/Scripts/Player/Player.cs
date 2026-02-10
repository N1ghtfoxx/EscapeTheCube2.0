using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private string playerName;
    [SerializeField] private int hydration = 10;
    [SerializeField] private int maxHydration = 10;

    private Field currentField;

    [Header("Card Inventory")]
    private int hintCards = 0;
    private int accessCards = 0;

    [Header("Special States")]
    private bool nextTurnMandatory = false;
    private bool hasVisitedWC = false;

    // moves player to a new field
    public void MoveToField(Field newField)
    {
        // signal that player moved this turn
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

        // reset mandatory turn flag after movement
        if (nextTurnMandatory)
        {
            nextTurnMandatory = false;
            Debug.Log($"{GetPlayerName()} completed mandatory turn (Energy Drink effect ended).");
        }

        currentField = newField;
        transform.position = newField.transform.position;

        // notify the field that player arrived
        newField.OnPlayerArrived(this);

        // switch to the next player
        GameManager.Instance.NextPlayer();
    }

    // returns the field the player is currently on
    public Field GetCurrentField()
    {
        return currentField;
    }

    // sets the player's starting station
    public void SetCurrentField(Field field)
    {
        currentField = field;

        // move player to the field's position
        if (field != null)
        {
            transform.position = field.transform.position;
        }
    }

    // changes the player's hydration level
    public void ChangeHydration(int amount)
    {
        hydration = Mathf.Clamp(hydration + amount, 0, maxHydration);
        OnHydrationChanged();
    }

    // returns current hydration value 
    public int GetHydration()
    {
        return hydration;
    }

    // returns max hydration value
    public int GetMaxHydration()
    {
        return maxHydration;
    }

    // called when hydration changes
    // - can be used for UI updates or other systems
    protected virtual void OnHydrationChanged()
    {
        // empty by default - can be extended by other systems
    }

    // returns the player's name
    public string GetPlayerName()
    {
        return playerName;
    }

    #region Hint Card Management

    public int GetHintCards()
    {
        return hintCards;
    }

    public void AddHintCard(int amount = 1)
    {
        hintCards += amount;
        Debug.Log($"{GetPlayerName()} gained {amount} hint card(s). Total: {hintCards}");
    }

    public void RemoveHintCard(int amount = 1)
    {
        hintCards = Mathf.Max(0, hintCards - amount);
        Debug.Log($"{GetPlayerName()} lost {amount} hint card(s). Total: {hintCards}");
    }

    #endregion

    #region Access Card Management

    public int GetAccessCards()
    {
        return accessCards;
    }

    public bool HasAccessCard()
    {
        return accessCards > 0;
    }

    public void AddAccessCard(int amount = 1)
    {
        accessCards += amount;
        Debug.Log($"{GetPlayerName()} gained {amount} access card(s). Total: {accessCards}");
    }

    public void ConsumeAccessCard(int amount = 1)
    {
        accessCards = Mathf.Max(0, accessCards - amount);
        Debug.Log($"{GetPlayerName()} consumed {amount} access card(s). Total: {accessCards}");
    }

    #endregion

    #region WC and Win Conditions

    public bool CanEnterWC()
    {
        return hintCards >= 3;
    }

    public bool HasVisitedWC()
    {
        return hasVisitedWC;
    }

    public void SetHasVisitedWC(bool value)
    {
        hasVisitedWC = value;
    }

    public bool CanWin()
    {
        return hintCards >= 3 && hasVisitedWC;
    }

    #endregion

    #region Special Effect Activators

    public void SetNextTurnMandatory(bool value)
    {
        nextTurnMandatory = value;
        if (value)
        {
            Debug.Log($"{GetPlayerName()}'s next turn is mandatory.");
        }
    }

    public bool IsNextTurnMandatory()
    {
        return nextTurnMandatory;
    }

    // Placeholder for Secret Passage (teleport to adjacent Theke/counter)
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

    // Placeholder for Free Move Towards Exit
    // Note: This should be handled by CardManager directly calling the movement logic
    // without triggering hydration loss through the power outage system
    public void ActivateFreeMoveTowardsExit()
    {
        Debug.Log($"{GetPlayerName()} can make one free move towards the exit.");
        // TODO: CardManager should handle this by temporarily activating power outage
        // or by directly moving the player without calling MoveToField
    }

    #endregion
}