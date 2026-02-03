using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private string playerName;
    [SerializeField] private int hydration = 10;
    [SerializeField] private int maxHydration = 10;

    private Field currentField;

    // moves player to a new field
    public void MoveToField(Field newField)
    {
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
        //currentField = field;
        //transform.position = field.transform.position;
        currentField = field;

        // WICHTIG: Bewege den Player visuell zur Station
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
}
