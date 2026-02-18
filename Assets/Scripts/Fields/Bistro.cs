using UnityEngine;

/// <summary>
/// Bistro field - requires an access card to enter
/// </summary>
public class Bistro : Field
{
    protected override void OnFieldClicked()
    {
        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

        // Check if player has access card
        if (!currentPlayer.HasAccessCard())
        {
            Debug.Log($"{currentPlayer.GetPlayerName()} needs an access card to enter the Bistro!");
            return;
        }

        // CRITICAL: Consume access card BEFORE moving (before NextPlayer() is called)
        // If we consume after base.OnFieldClicked(), the wrong player loses the card!
        currentPlayer.ConsumeAccessCard();
        Debug.Log($"{currentPlayer.GetPlayerName()} used an access card to enter the Bistro.");

        // Allow entry - this calls MoveToField() which triggers NextPlayer()
        base.OnFieldClicked();
    }

    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);

        // Access card was already consumed in OnFieldClicked()
        // Just log the arrival for clarity
        Debug.Log($"{player.GetPlayerName()} has entered the Bistro.");

        // start dice roll
        DiceManager.Instance.OnDiceResult.AddListener(OnBistroDiceResult);
        DiceManager.Instance.RollDice();
    }

    private void OnBistroDiceResult(int result)
    {
        // remove Listener instantly
        DiceManager.Instance.OnDiceResult.RemoveListener(OnBistroDiceResult);

        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

        if (result == 2 || result == 6)
        {
            currentPlayer.AddHintCard();
            Debug.Log($"{currentPlayer.GetPlayerName()} rolled {result} in Bistro - gained a hint card!");
        }
        else
        {
            Debug.Log($"{currentPlayer.GetPlayerName()} rolled {result} in Bistro - no reward.");
        }
    }
}