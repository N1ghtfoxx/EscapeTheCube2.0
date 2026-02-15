using UnityEngine;

public class Exit : Field
{
    protected override void OnFieldClicked()
    {
        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

        // check win conditions
        if (!currentPlayer.CanWin())
        {
            if (currentPlayer.GetHintCards() < 3)
            {
                Debug.Log($"{currentPlayer.GetPlayerName()} needs 3 hint cards to exit. Current: {currentPlayer.GetHintCards()}");
            }
            else if (!currentPlayer.HasVisitedWC())
            {
                Debug.Log($"{currentPlayer.GetPlayerName()} must visit the WC before exiting!");
            }
            return;
        }
        // player meets all conditions to win
        base.OnFieldClicked();
    }

    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);

        if (player.CanWin())
        {
            Debug.Log($"{player.GetPlayerName()} HAS WON THE GAME!");
            /// TODO: win screen?
            HandlePlayerWin(player);
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} cannot exit yet. Requirements: 3 hints + WC visit");
        }
    }

    public void HandlePlayerWin(Player winner)
    {
        // disable all fields
        Field[] allFields = FindObjectsByType<Field>(FindObjectsSortMode.None);
        foreach (Field field in allFields)
        {
            field.SetClickable(false);
        }

        Debug.Log($"Game Over! Winner: {winner.GetPlayerName()}");
        /// TODO: show win screen, stop game, etc..
    }
}
