// made by Naomi in collaboration with Claude Ai

using UnityEngine;

/// <summary>
/// WC field
///
/// Behaviour on arrival:
///   - Player has 3 or more hint cards: grants +1 hydration and unlocks the Exit
///   - Player has fewer than 3 hint cards: arrival has no effect; a debug message is logged
/// </summary>
public class WC : Field
{
    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);

        if (player.CanEnterWC())
        {
            // Requirements met — reward the player and mark the WC as visited
            player.ChangeHydration(1);
            player.SetHasVisitedWC(true);
            Debug.Log($"{player.GetPlayerName()} visited the WC! +1 Hydration. Exit is now accessible!");
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} needs 3 hint cards to use the WC. " +
                      $"Current: {player.GetHintCards()}");
        }
    }
}