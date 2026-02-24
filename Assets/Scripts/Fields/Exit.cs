// made by Naomi in collaboration with Claude Ai

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Exit field — the winning tile of the board
///
/// Entry is gated behind the win conditions (3 hint cards + WC visit)
/// When a qualifying player arrives, the game ends: the winner is recorded,
/// all other players receive a loss, and the Game Over screen is shown
/// </summary>
public class Exit : Field
{
    // -------------------------------------------------------------------------
    // Field Callbacks
    // -------------------------------------------------------------------------

    protected override void OnFieldClicked()
    {
        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

        // Enforce win conditions before allowing movement
        if (!currentPlayer.CanWin())
        {
            if (currentPlayer.GetHintCards() < 3)
            {
                Debug.Log($"{currentPlayer.GetPlayerName()} needs 3 hint cards to exit. " +
                          $"Current: {currentPlayer.GetHintCards()}");
            }
            else if (!currentPlayer.HasVisitedWC())
            {
                Debug.Log($"{currentPlayer.GetPlayerName()} must visit the WC before exiting!");
            }
            return; // Block the move
        }

        // All conditions met — proceed with normal movement
        base.OnFieldClicked();
    }

    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);

        if (player.CanWin())
        {
            Debug.Log($"{player.GetPlayerName()} HAS WON THE GAME!");
            HandlePlayerWin(player);
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} cannot exit yet. Requirements: 3 hints + WC visit");
        }
    }

    // -------------------------------------------------------------------------
    // Win Handling
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ends the game with the given player as the winner
    ///   1. Disables all field interactions
    ///   2. Records Win/Loss stats in the database for every player
    ///   3. Shows the Game Over screen
    /// </summary>
    public void HandlePlayerWin(Player winner)
    {
        int rounds = GameManager.Instance.GetRoundCount();
        int playtime = GameManager.Instance.GetPlaytimeSeconds();

        // Prevent any further field interaction
        Field[] allFields = FindObjectsByType<Field>(FindObjectsSortMode.None);
        foreach (Field field in allFields)
            field.SetClickable(false);

        // Record the winner in the database
        if (DBManager.Instance != null)
        {
            DBManager.Instance.UpdatePlayerStats(
                winner.GetPlayerName(),
                rounds,
                GameResult.Win,
                playtime
            );
        }

        // Record every other active player as a loss
        foreach (Player player in GameManager.Instance.GetAllPlayers())
        {
            if (player == winner) continue;

            if (DBManager.Instance != null)
            {
                DBManager.Instance.UpdatePlayerStats(
                    player.GetPlayerName(),
                    rounds,
                    GameResult.Loss,
                    playtime
                );
            }
        }

        Debug.Log($"Game Over! Winner: {winner.GetPlayerName()}");

        // Show the end screen — includes the winner and all other players (even eliminated ones)
        if (GameOverScreenManager.Instance != null)
        {
            List<PlayerStats> allPlayerStats = GameManager.Instance.GetAllPlayersStats(winner);
            GameOverScreenManager.Instance.ShowEndScreen(winner, allPlayerStats);
        }
        else
        {
            Debug.LogError("GameOverScreenManager.Instance is null!");
        }
    }
}