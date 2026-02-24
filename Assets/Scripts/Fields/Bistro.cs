// made by Naomi in collaboration with Claude Ai

using UnityEngine;

/// <summary>
/// Bistro field — always accessible, no entry conditions
///
/// Behaviour on arrival:
///   - WITH access card: card is consumed and a bonus dice roll is triggered
///       -> Roll 2 or 6: player gains one hint card
///       -> Any other result: no additional reward
///   - WITHOUT access card: player simply enters; no bonus roll
/// </summary>
public class Bistro : Field
{
    // Cached reference to the player who just arrived
    // Ensures OnBistroDiceResult always rewards the correct player,
    // even if GetCurrentPlayer() has already advanced by the time the result fires
    private Player _arrivingPlayer;

    // -------------------------------------------------------------------------
    // Field Callbacks
    // -------------------------------------------------------------------------

    protected override void OnFieldClicked()
    {
        // Bistro is always enterable — no guard condition needed
        base.OnFieldClicked();
    }

    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);

        if (player.HasAccessCard())
        {
            // Consume the access card and kick off the bonus roll
            player.ConsumeAccessCard();
            Debug.Log($"{player.GetPlayerName()} used an access card in the Bistro – rolling for a bonus hint card!");

            _arrivingPlayer = player;
            DiceManager.Instance.OnDiceResult.AddListener(OnBistroDiceResult);
            DiceManager.Instance.RollDice();
        }
        else
        {
            // No access card — just enter with no bonus effect
            Debug.Log($"{player.GetPlayerName()} entered the Bistro (no access card – no bonus roll).");
            GameManager.Instance.SetPlayerMoved(true);
        }
    }

    // -------------------------------------------------------------------------
    // Dice Callback
    // -------------------------------------------------------------------------

    /// <summary>
    /// Receives the bonus dice result after an access-card entry
    /// Awards a hint card on a roll of 2 or 6, then marks the player as moved
    /// </summary>
    private void OnBistroDiceResult(int result)
    {
        // Unsubscribe immediately — this is a one-shot listener
        DiceManager.Instance.OnDiceResult.RemoveListener(OnBistroDiceResult);

        Player player = _arrivingPlayer;
        _arrivingPlayer = null;

        if (result == 2 || result == 6)
        {
            player.AddHintCard();
            Debug.Log($"{player.GetPlayerName()} rolled {result} in Bistro – gained a bonus hint card!");
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} rolled {result} in Bistro – no bonus hint card.");
        }

        GameManager.Instance.SetPlayerMoved(true);
    }
}