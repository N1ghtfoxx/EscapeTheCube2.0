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

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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

    // switches to the next player
    public void NextPlayer()
    {
        // Check if current player has mandatory next turn (e.g., cannot skip)
        Player currentPlayer = GetCurrentPlayer();
        if (currentPlayer.IsNextTurnMandatory())
        {
            // If mandatory, do not advance; reset flag and force move
            currentPlayer.SetNextTurnMandatory(false);
            Debug.Log($"{currentPlayer.GetPlayerName()} must take their mandatory turn.");
            // TODO: If turns can be skipped, prevent skip here; otherwise, just log
            return;
        }
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
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
            player.ConsumeAccessCard(); // Assume consume on successful entry
            Debug.Log($"{player.GetPlayerName()} entered Bistro using an access card.");
            // TODO: Additional Bistro logic if needed
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
}
