using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private Field startField;
    private List<Field> allFields = new List<Field>();

    private int currentPlayerIndex = 0;

    // Power Outage Tracking
    private bool isPowerOutageActive = false;
    private int powerOutageRemainingPlayers = 0;

    // for mandatory turn logic
    private bool playerMovedThisTurn = false; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        // find all fields in the scene
        allFields = FindObjectsByType<Field>(FindObjectsSortMode.None).ToList();

        if (players.Count > 0 && startField != null)
        {
            players[0].SetCurrentField(startField);
        }
        UpdateClickableFields();
    }

    // returns the player whose turn it currently is
    public Player GetCurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    // called by player when they move
    public void SetPlayerMoved(bool moved)
    {
        playerMovedThisTurn = moved;
    }

    // called by CardManager/UI to check if player oved
    public bool DidPlayerMoveThisTurn()
    {
        return playerMovedThisTurn;
    }

    // switches to the next player
    public void NextPlayer()
    {
        Player currentPlayer = GetCurrentPlayer();
        
        // check if current player has mandatory next turn
        if (currentPlayer.IsNextTurnMandatory())
        {
            Debug.Log($"{currentPlayer.GetPlayerName()} must take their mandatory turn (Energy Drink effect). Cannot skip!");
            return;
        }

        // reset movement flag when switching to next player
        playerMovedThisTurn = false;

        // Switch to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        // Handle Power Outage countdown AFTER switching player
        if (isPowerOutageActive)
        {
            powerOutageRemainingPlayers--;
            Debug.Log($"Power Outage: {powerOutageRemainingPlayers} player(s) remaining in this round.");

            if (powerOutageRemainingPlayers <= 0)
            {
                isPowerOutageActive = false;
                Debug.Log("Power Outage ended - normal hydration loss applies again.");
            }
        }

        UpdateClickableFields();
    }

    // makes fields clickable based on current player position
    public void UpdateClickableFields()
    {
        // first, make all fields non-clickable
        foreach (Field field in allFields)
        {
            field.SetClickable(false);
        }

        // get current player and their position
        Player currentPlayer = GetCurrentPlayer();
        Field currentField = currentPlayer.GetCurrentField();

        // make neighboring fields clickable
        if (currentField != null)
        {
            List<Field> neighbours = currentField.GetNeighbours();
            foreach (Field neighbour in neighbours)
            {
                neighbour.SetClickable(true);
            }
        }
    }

    // returns all fields that currently have players on them
    public List<Field> GetOccupiedFields()
    {
        List<Field> occupied = new List<Field>();
        foreach (Player player in players)
        {
            Field field = player.GetCurrentField();
            if (field != null && !occupied.Contains(field))
            {
                occupied.Add(field);
            }
        }
        return occupied;
    }

    // returns list of all players in the game
    public List<Player> GetAllPlayers()
    {
        return players;
    }

    // Additional method for Bistro entry (called from Bistro.cs)
    public void TryBistroEntry(Player player)
    {
        if (player.HasAccessCard())
        {
            player.ConsumeAccessCard();
            Debug.Log($"{player.GetPlayerName()} entered Bistro using an access card.");
        }
        else
        {
            Debug.Log($"{player.GetPlayerName()} cannot enter Bistro without access card.");
        }
    }

    // Helper for finding player with most hint cards (for CardManager)
    public Player GetPlayerWithMostHintCards()
    {
        Player maxPlayer = null;
        int maxHints = -1;
        foreach (Player player in players)
        {
            int hints = player.GetHintCards();
            if (hints > maxHints)
            {
                maxHints = hints;
                maxPlayer = player;
            }
        }
        return maxPlayer;
    }

    // Power Outage - Called by CardManager when power outage card is drawn
    // Enables no hydration loss for all players for one complete round
    public void EnableNoHydrationLossForAllPlayersThisTurn()
    {
        isPowerOutageActive = true;
        // +1 to include the current player who drew the card
        powerOutageRemainingPlayers = players.Count + 1;

        Debug.Log($"Power Outage activated! No hydration loss for the next {powerOutageRemainingPlayers} moves.");
    }

    // Check if power outage is currently active
    public bool IsPowerOutageActive()
    {
        return isPowerOutageActive;
    }
}