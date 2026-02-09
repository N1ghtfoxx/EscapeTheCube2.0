using UnityEngine;

public class Bistro : Field
{
    protected override void OnFieldClicked()
    {
        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

        // check if player has access card 
        if (!currentPlayer.HasAccessCard())
        {
            Debug.Log($"{currentPlayer.GetPlayerName()} needs an access card to enter the Bistro!");
            return;
        }

        // allow entry
        base.OnFieldClicked();
    }

    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);

        GameManager.Instance.TryBistroEntry(player); 
    }
}
