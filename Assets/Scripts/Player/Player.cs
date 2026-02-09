using UnityEngine;
using UnityEngine.Rendering;

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
    private bool noHydrationLossThisTurn = false;

    // moves player to a new field
    public void MoveToField(Field newField)
    {
        Debug.LogWarning($"{GetPlayerName()} - MoveToField aufgerufen | Flag-Wert = {noHydrationLossThisTurn} | Hydration vor Move: {GetHydration()}");

        if (!noHydrationLossThisTurn)
        {
            ChangeHydration(-1);
            Debug.Log("-1 Hydration abgezogen");
        }
        else
        {
            Debug.Log("KEIN Hydrationsverlust – Flag war true");
            noHydrationLossThisTurn = false;
            Debug.Log("Flag zurückgesetzt auf false");
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

    // public for CardManager to call
    public void SetNoHydrationLossThisTurn(bool value)
    {
        noHydrationLossThisTurn = value;
        Debug.LogError("SETTER LÄUFT! Bei " + GetPlayerName() + " -> Wert = " + value);
    }

    public bool GetNoHydrationLossThisTurn()
    {
        return noHydrationLossThisTurn;
    }


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
    // Assuming "adjacent Theke" means a specific Field type like Theke1;
    // implement logic here or in GameManager
    public void ActivateSecretPassage()
    {
        // TODO: Logic to find and teleport to an adjacent Theke (e.g., Theke1 or similar)
        // For example: Find nearest Theke1 from currentField neighbours
        Field targetTheke = null;
        foreach (Field neighbour in currentField.GetNeighbours())
        {
            if (neighbour is Theke1) // Assuming Theke1 is the "counter"
            {
                targetTheke = neighbour;
                break;
            }
        }
        if (targetTheke != null)
        {
            MoveToField(targetTheke); // Use MoveToField to handle movement
            Debug.Log($"{GetPlayerName()} teleported to adjacent Theke via secret passage.");
        }
        else
        {
            Debug.Log($"{GetPlayerName()} no adjacent Theke found for secret passage.");
        }
    }

    // Placeholder for Free Move Towards Exit
    public void ActivateFreeMoveTowardsExit()
    {
        // TODO: Logic to move one step towards Exit without cost
        // This might require pathfinding or predefined paths to Exit
        // For now, assume we set noHydrationLossThisTurn and prompt a move
        SetNoHydrationLossThisTurn(true);
        Debug.Log($"{GetPlayerName()} can make one free move towards the exit.");
        // Additional logic if automatic move is needed
    }

    #endregion
}
