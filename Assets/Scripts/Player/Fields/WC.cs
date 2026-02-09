using UnityEngine;

public class WC : Field
{
    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);

        if (player.CanEnterWC())
        {
            // player has 3 hints - grabt +1 hydration and unlock exit
            player.ChangeHydration(1);
            player.SetHasVisitedWC(true);
            Debug.Log($"{player.GetPlayerName()} visited the WC! +1 Hydration. Exit is now accessible!");
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} needs 3 hint cards to use the WC. Current: {player.GetHintCards()}");
        }
    }
}
