using UnityEngine;

/// <summary>
/// Bistro field - always enterable.
/// If the player has an access card, it is consumed and a dice roll is triggered:
///   ? result 2 or 6: gain a hint card (bonus reward)
/// Without an access card the player simply enters with no additional effect.
/// </summary>
public class Bistro : Field
{
    // Cache the arriving player so OnBistroDiceResult always rewards the correct
    // player, even if GetCurrentPlayer() has already advanced by then.
    private Player _arrivingPlayer;

    protected override void OnFieldClicked()
    {
        // Bistro is always accessible - no access card required to enter
        base.OnFieldClicked();
    }

    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);

        if (player.HasAccessCard())
        {
            // Consume the access card and trigger bonus dice roll for a hint card
            player.ConsumeAccessCard();
            Debug.Log($"{player.GetPlayerName()} used an access card in the Bistro - rolling for a bonus hint card!");

            _arrivingPlayer = player;
            DiceManager.Instance.OnDiceResult.AddListener(OnBistroDiceResult);
            DiceManager.Instance.RollDice();
        }
        else
        {
            // No access card - just enter, no bonus roll
            Debug.Log($"{player.GetPlayerName()} entered the Bistro (no access card - no bonus roll).");
            GameManager.Instance.SetPlayerMoved(true);
        }
    }

    private void OnBistroDiceResult(int result)
    {
        DiceManager.Instance.OnDiceResult.RemoveListener(OnBistroDiceResult);

        Player player = _arrivingPlayer;
        _arrivingPlayer = null;

        if (result == 2 || result == 6)
        {
            player.AddHintCard();
            Debug.Log($"{player.GetPlayerName()} rolled {result} in Bistro - gained a bonus hint card!");
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} rolled {result} in Bistro - no bonus hint card.");
        }

        GameManager.Instance.SetPlayerMoved(true);
    }
}