using UnityEngine;

/// <summary>
/// Bistro field - requires an access card to enter
/// </summary>
public class Bistro : Field
{
    //// stores the lambda so we can remove it from the listener later
    //private System.Action<int> _bistroDiceCallback;

    // cache the arriving player so OnBistroDiceResult always rewards the correct player,
    // even if GetCurrentPlayer() has already advanced to the next player by then
    private Player _arrivingPlayer;

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
        // cache player NOW before turn potentially advances
        _arrivingPlayer = player;
        DiceManager.Instance.OnDiceResult.AddListener(OnBistroDiceResult);
        DiceManager.Instance.RollDice();
    }

    private void OnBistroDiceResult(int result)
    {
        // remove Listener instantly using stored reference
        DiceManager.Instance.OnDiceResult.RemoveListener(OnBistroDiceResult);

        //Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

        // use cached player instead of GetCurrentPlayer()
        // GetCurrentPlayer() could already point to the next player at this point
        Player player = _arrivingPlayer;
        _arrivingPlayer = null;

        if (result == 2 || result == 6)
        {
            player.AddHintCard();
            Debug.Log($"{player.GetPlayerName()} rolled {result} in Bistro - gained a hint card!");
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} rolled {result} in Bistro - no reward.");
        }

        GameManager.Instance.SetPlayerMoved(true);
    }
}