using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager Instance { get; private set; }

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

    #endregion

    #region Serialized Fields

    [SerializeField] private GameObject playerPrefab; // the one single Playr Prefab
    [SerializeField] private Field startField;

    [Header("UI Panels")]
    [SerializeField] private PlayerUiPanel[] playerUiPanels; // assign panels in corret order (Player 1-4)

    #endregion

    #region Private Fields

    private List<Player> players = new List<Player>(); // now dynamic, populated at runtime
    private List<Field> allFields = new List<Field>();
    private int currentPlayerIndex = 0;

    // Power Outage Effect (global for all players, lasts one full round)
    private bool isPowerOutageActive = false;
    private int powerOutageRemainingPlayers = 0;

    // Movement Tracking (for card deck eligibility)
    private bool playerMovedThisTurn = false;

    // Game Lock (prevents interaction during dice rolls, animations, etc.)
    private bool isGameLocked = false;

    #endregion

    #region Unity Events

    public UnityEvent OnNextPlayer;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeGame();
        SubscribeToDiceEvents();
    }

    #endregion

    #region Initialization

    private void InitializeGame()
    {
        // Find all fields in the scene
        allFields = FindObjectsByType<Field>(FindObjectsSortMode.None).ToList();

        // spawn player from StartHub selection
        SpawnPlayerFromSelection();

        // Set starting position for first player
        if (players.Count > 0 && startField != null)
        {
            players[0].SetCurrentField(startField);
        }

        UpdateClickableFields();
        AssignPlayersToUIPanels();
    }

    /// <summary>
    /// Spawns all player characters selected in StartHub
    /// Uses single player prefab and applies selected character sprites
    /// </summary>
    private void SpawnPlayerFromSelection()
    {
        // check if PlayerData exists and has valid selections
        if (PlayerData.Instance == null || !PlayerData.Instance.HasSelection())
        {
            Debug.LogWarning("No player selection found! Using fallback player.");
            // FALLBACK: You could create a default player here or show error
            return;
        }

        // check if player prefab is assigned
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned in GameManager!");
            return;
        }

        // get all player selections
        var allSelections = PlayerData.Instance.GetAllPlayerSelections();

        Debug.Log($"Spawning {allSelections.Count} player(s)");

        // spawn each player
        for (int i = 0; i< allSelections.Count; i++)
        {
            var selection = allSelections[i];

            // calculate spawn position (offset if multiple players)
            Vector3 spawnPosition = startField != null ? startField.transform.position : Vector3.zero;

            // add small offset for multipe players so they don't overlap
            if (allSelections.Count > 1)
            {
                float offsetX = (i - (allSelections.Count - 1) / 2f) * 0.5f; // spread them out
                spawnPosition += new Vector3(offsetX, 0, 0);
            }

            // spawn the player prefab
            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerObject.name = $"Player_{i + 1}_{selection.playerName}"; // readable name in hierarchy

            // Get Player component and configure it
            Player playerComponent = playerObject.GetComponent<Player>();
            if (playerComponent != null)
            {
                // Set player name from StartHub input
                playerComponent.SetPlayerName(selection.playerName);

                // Apply character sprite to the spawned player
                SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && selection.characterData != null)
                {
                    spriteRenderer.sprite = selection.characterData.characterSprite;
                    Debug.Log($"Player {i + 1} applied sprite: {selection.characterData.characterSprite.name}");
                }

                //// Optional: Apply character-specific stats
                //if (selection.characterData != null && selection.characterData.startingHydration > 0)
                //{
                //    // If you want different starting hydration per character
                //    // You'd need to add a method like SetStartingHydration(int amount) to Player.cs
                //    // playerComponent.SetStartingHydration(selection.characterData.startingHydration);
                //}

                // Add to players list
                players.Add(playerComponent);

                Debug.Log($"Player {i + 1} spawned: {selection.playerName} as {selection.characterData.characterName}");
            }
            else
            {
                Debug.LogError("Spawned player prefab doesn't have Player component!");
                Destroy(playerObject);
            }
        }

        // Set all players to start field
        if (startField != null)
        {
            foreach (var player in players)
            {
                player.SetCurrentField(startField);
            }
        }
    }

    private void SubscribeToDiceEvents()
    {
        // Subscribe to dice events if DiceManager exists
        if (DiceManager.Instance != null)
        {
            // OnDiceRoll will be added by your teammate
            // Uncomment this line when DiceManager.OnDiceRoll is implemented:
            DiceManager.Instance.OnDiceRoll.AddListener(OnDiceRollStarted);

            // OnDiceResult already exists in DiceManager
            DiceManager.Instance.OnDiceResult.AddListener(OnDiceRollFinished);
        }
    }

    #endregion

    #region Player Management

    /// <summary>
    /// Returns the player whose turn it currently is
    /// </summary>
    public Player GetCurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    /// <summary>
    /// Returns list of all players in the game
    /// </summary>
    public List<Player> GetAllPlayers()
    {
        return players;
    }

    /// <summary>
    /// Switches to the next player and handles round-based effects
    /// </summary>
    public void NextPlayer()
    {
        // Reset movement flag when switching to next player
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

        OnNextPlayer?.Invoke();

        UpdateClickableFields();
    }

    #endregion

    #region Field Management

    /// <summary>
    /// Makes fields clickable based on current player position
    /// Respects game lock state (e.g., during dice rolls)
    /// </summary>
    public void UpdateClickableFields()
    {
        // If game is locked (e.g., during dice roll), don't make anything clickable
        if (isGameLocked)
        {
            Debug.Log("Game is locked - fields remain non-clickable");
            return;
        }

        // First, make all fields non-clickable
        foreach (Field field in allFields)
        {
            field.SetClickable(false);
        }

        // Get current player and their position
        Player currentPlayer = GetCurrentPlayer();
        Field currentField = currentPlayer.GetCurrentField();

        // Make neighboring fields clickable
        if (currentField != null)
        {
            List<Field> neighbours = currentField.GetNeighbours();
            foreach (Field neighbour in neighbours)
            {
                neighbour.SetClickable(true);
            }
        }
    }

    /// <summary>
    /// Returns all fields that currently have players on them
    /// </summary>
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

    #endregion

    #region Special Field Actions

    /// <summary>
    /// Handles Bistro entry logic (called from Bistro.cs)
    /// Checks if player has access card and consumes it
    /// </summary>
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

    #endregion

    #region Card Effects - Power Outage

    /// <summary>
    /// Activates Power Outage effect (called by CardManager)
    /// Prevents hydration loss for all players for one complete round
    /// </summary>
    public void EnableNoHydrationLossForAllPlayersThisTurn()
    {
        isPowerOutageActive = true;
        powerOutageRemainingPlayers = players.Count + 1; // +1 to include current player

        Debug.Log($"Power Outage activated! No hydration loss for the next {powerOutageRemainingPlayers} moves.");
    }

    /// <summary>
    /// Checks if Power Outage is currently active (called by Player.MoveToField)
    /// </summary>
    public bool IsPowerOutageActive()
    {
        return isPowerOutageActive;
    }

    #endregion

    #region Card Effects - Movement Tracking

    /// <summary>
    /// Sets whether the current player moved this turn (called by Player.MoveToField)
    /// Used to determine which card decks are available
    /// </summary>
    public void SetPlayerMoved(bool moved)
    {
        playerMovedThisTurn = moved;
    }

    /// <summary>
    /// Checks if current player moved this turn (called by CardManager/UI)
    /// Returns true if player moved, false otherwise
    /// Used for card deck eligibility: both decks if moved, only action cards if not
    /// </summary>
    public bool DidPlayerMoveThisTurn()
    {
        return playerMovedThisTurn;
    }

    #endregion

    #region Card Effects - Energy Drink

    /// <summary>
    /// Checks if current player can skip movement (called by CardManager/UI)
    /// Returns false if Energy Drink effect is active (mandatory move required)
    /// Used to disable card buttons when player must move
    /// </summary>
    public bool CanCurrentPlayerSkipMovement()
    {
        Player currentPlayer = GetCurrentPlayer();
        return !currentPlayer.IsNextTurnMandatory();
    }

    #endregion

    #region Helper Methods for CardManager

    /// <summary>
    /// Finds and returns the player with the most hint cards
    /// Used by CardManager for certain card effects
    /// </summary>
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

    #endregion

    #region Dice Events (Game Lock System)

    /// <summary>
    /// Called when dice roll starts (by DiceManager.OnDiceRoll event)
    /// Locks game and disables all field interactions
    /// </summary>
    private void OnDiceRollStarted()
    {
        isGameLocked = true;
        Debug.Log("Dice rolling - game locked!");

        // Make all fields non-clickable during dice roll
        foreach (Field field in allFields)
        {
            field.SetClickable(false);
        }
    }

    /// <summary>
    /// Called when dice roll finishes (by DiceManager.OnDiceResult event)
    /// Unlocks game and restores field interactions
    /// </summary>
    private void OnDiceRollFinished(int result)
    {
        Debug.Log($"Dice result: {result} - unlocking game!");

        isGameLocked = false;

        // Restore clickable fields
        UpdateClickableFields();

        // TODO: Handle dice result (e.g., spawn/move Alf)
        // This is handled by your teammate's code
    }

    /// <summary>
    /// Checks if game is currently locked (called by UI)
    /// Returns true during dice rolls, animations, or other blocking events
    /// </summary>
    public bool IsGameLocked()
    {
        return isGameLocked;
    }

    #endregion

    private void AssignPlayersToUIPanels()
    {
        // Use explicit panel array if assigned, otherwise find them
        PlayerUiPanel[] panels = playerUiPanels != null && playerUiPanels.Length > 0
            ? playerUiPanels
            : FindObjectsByType<PlayerUiPanel>(FindObjectsSortMode.None);

        List<Player> activePlayers = GetAllPlayers();

        // Safety check
        if (panels == null || panels.Length == 0)
        {
            Debug.LogError("No PlayerUiPanels found! Assign them in GameManager or add them to the scene.");
            return;
        }

        // Zuerst alle UIs ausblenden
        foreach (var panel in panels)
        {
            if (panel != null)
            {
                panel.Hide();
                panel.AssignPlayer(null);   // alte Zuweisung löschen
            }
        }

        // Nur echte, aktive Spieler zuweisen (aktuell 1-4 möglich)
        for (int i = 0; i < activePlayers.Count && i < panels.Length; i++)
        {
            if (panels[i] == null)
            {
                Debug.LogWarning($"Panel {i} is null in playerUIPanels array!");
                continue;
            }

            var player = activePlayers[i];
            var panel = panels[i];

            panel.AssignPlayer(player);
            panel.Show();                             // nur zugewiesene Panels einblenden

            Debug.Log($"UI Assignment: {player.GetPlayerName()} → Panel {i + 1}");
        }

        // Initial-Update der sichtbaren Panels
        foreach (var player in activePlayers)
        {
            UiManager.Instance?.UpdatePlayerUI(player);
        }

        Debug.Log($"UI-Zuweisung: {activePlayers.Count} Spieler → {activePlayers.Count} UI-Panels sichtbar");
    }
}