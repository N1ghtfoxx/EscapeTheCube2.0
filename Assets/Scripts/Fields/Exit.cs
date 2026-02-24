//using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

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
            HandlePlayerWin(player);
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} cannot exit yet. Requirements: 3 hints + WC visit");
        }
    }

    public void HandlePlayerWin(Player winner)
    {
        int rounds = GameManager.Instance.GetRoundCount();
        int playtime = GameManager.Instance.GetPlaytimeSeconds();

        // disable all fields
        Field[] allFields = FindObjectsByType<Field>(FindObjectsSortMode.None);
        foreach (Field field in allFields)
            field.SetClickable(false);

        // track winner in DB
        if (DBManager.Instance != null)
        {
            DBManager.Instance.UpdatePlayerStats(
                winner.GetPlayerName(),
                rounds,
                GameResult.Win,
                playtime
            );
        }

        // track every other player as loss
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

        // Show Game Over screen with winner and ALL players (including eliminated ones)
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
