using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

    [SerializeField] private GameObject playerPrefab; // Your single player prefab
    [SerializeField] private Field startField;

    [Header("UI Panels")]
    [SerializeField] private PlayerUiPanel[] playerUIPanels; // Assign panels in correct order (Player 1-4)

    #endregion

    #region Private Fields

    private List<Player> players = new List<Player>(); // Now dynamic, populated at runtime
    private List<Field> allFields = new List<Field>();
    private int currentPlayerIndex = 0;

    // Power Outage Effect (global for all players, lasts one full round)
    private bool isPowerOutageActive = false;
    private int powerOutageRemainingPlayers = 0;

    // Movement Tracking (for card deck eligibility)
    private bool playerMovedThisTurn = false;

    // Game Lock (prevents interaction during dice rolls, animations, etc.)
    private bool isGameLocked = false;

    // statistics for DB
    private int roundCount = 0;
    private float gameStartTime;

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

        // start playtime tracking
        gameStartTime = Time.time;

        roundCount = 1;

        // Spawn players from StartHub selection
        SpawnPlayersFromSelection();

        // Set starting position for all spawned players
        if (players.Count > 0 && startField != null)
        {
            players[0].SetCurrentField(startField);
        }

        UpdateClickableFields();
        AssignPlayersToUIPanels();
    }

    /// <summary>
    /// Spawns all player characters selected in StartHub.
    /// Uses a single player prefab and applies the chosen CharacterData sprite per player.
    /// </summary>
    private void SpawnPlayersFromSelection()
    {
        if (PlayerData.Instance == null || !PlayerData.Instance.HasSelection())
        {
            Debug.LogError("[GameManager] No player selection found in PlayerData! " +
                           "Make sure players complete the StartScreen before entering MainScene.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] Player Prefab is not assigned in GameManager!");
            return;
        }

        var allSelections = PlayerData.Instance.GetAllPlayerSelections();
        Debug.Log($"[GameManager] Spawning {allSelections.Count} player(s) from StartHub selection.");

        for (int i = 0; i < allSelections.Count; i++)
        {
            var selection = allSelections[i];

            // Spawn at field center – Player.RepositionPlayersOnField() handles
            // the final layout (horizontal row, centered) once all players are registered.
            Vector3 spawnPosition = startField != null ? startField.transform.position : Vector3.zero;

            // Spawn the player prefab
            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerObject.name = $"Player_{i + 1}_{selection.playerName}";

            // Get Player component and configure it
            Player playerComponent = playerObject.GetComponent<Player>();
            if (playerComponent != null)
            {
                // Set player name from StartHub input
                playerComponent.SetPlayerName(selection.playerName);

                // Apply character sprite
                SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && selection.characterSprite != null)
                {
                    spriteRenderer.sprite = selection.characterSprite;
                    spriteRenderer.color = Color.white; // Preserve original sprite colours
                    Debug.Log($"[GameManager] Player {i + 1} sprite applied: {selection.characterSprite.name}");
                }


                players.Add(playerComponent);
                Debug.Log($"[GameManager] Player {i + 1} spawned: {selection.playerName}");
            }
            else
            {
                Debug.LogError("[GameManager] Spawned player prefab doesn't have a Player component!");
                Destroy(playerObject);
            }
        }

        // All players are now registered – assign them to the start field WITH repositioning.
        // Player.SetCurrentField(moveToPosition: true) calls RepositionPlayersOnField()
        // which centers the full group in a horizontal row on the field.
        if (startField != null)
        {
            foreach (var player in players)
            {
                player.SetCurrentField(startField, moveToPosition: true);
            }
        }
    }

    private void SubscribeToDiceEvents()
    {
        if (DiceManager.Instance != null)
        {
            DiceManager.Instance.OnDiceRoll.AddListener(OnDiceRollStarted);
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
        // Switch to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        if (currentPlayerIndex == 0)
        {
            roundCount++;
            Debug.Log($"Round {roundCount} started.");
        }

        // Reset movement flag for the NEW player BEFORE event
        // This ensures CardButtonController sees the correct state for the new player
        playerMovedThisTurn = false;

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

        // Invoke event AFTER resetting movement flag
        // CardButtonController will now see playerMovedThisTurn = false for new player
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
    /// Disables all fields (called after player movement)
    /// Player can only draw cards after moving, not move again
    /// Fields will be re-enabled when next player's turn starts
    /// </summary>
    public void DisableAllFields()
    {
        foreach (Field field in allFields)
        {
            field.SetClickable(false);
        }
        Debug.Log("All fields disabled - player can now only draw cards");
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

        // Immediately update card button states after movement
        UpdateCardButtons();
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

    /// <summary>
    /// Updates the card button states by finding and calling CardButtonController
    /// </summary>
    private void UpdateCardButtons()
    {
        CardButtonController controller = FindFirstObjectByType<CardButtonController>();
        if (controller != null)
        {
            controller.UpdateCardButtonStates();
        }
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

    /// <summary>
    /// Called when a card is drawn (by CardManager button handlers)
    /// Ends the current player's turn by calling NextPlayer()
    /// NOTE: Some cards (like Secret Passage) handle their own NextPlayer() call in Player.cs
    /// </summary>
    public void OnCardDrawn()
    {
        // termination if no player left
        if (players.Count == 0) return;

        Debug.Log($"{GetCurrentPlayer().GetPlayerName()} drew a card - ending turn.");
        NextPlayer();
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

        if (!playerMovedThisTurn)
        {
            UpdateClickableFields();
        }
        else
        {
            Debug.Log("Player already moved this turn - fields stay disabled after dice roll.");
        }
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
        PlayerUiPanel[] panels = playerUIPanels != null && playerUIPanels.Length > 0
            ? playerUIPanels
            : FindObjectsByType<PlayerUiPanel>(FindObjectsSortMode.None);

        List<Player> activePlayers = GetAllPlayers();

        if (panels == null || panels.Length == 0)
        {
            Debug.LogError("No PlayerUiPanels found! Assign them in GameManager or add them to the scene.");
            return;
        }

        // Hide all panels first
        foreach (var panel in panels)
        {
            if (panel != null)
            {
                panel.Hide();
                panel.AssignPlayer(null);
            }
        }

        // Sprite-based assignment: match player sprite to panel sprite
        foreach (var player in activePlayers)
        {
            SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
            if (playerSprite == null || playerSprite.sprite == null)
            {
                Debug.LogWarning($"Player {player.GetPlayerName()} has no SpriteRenderer or sprite!");
                continue;
            }

            bool matched = false;
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;
                if (panels[i].GetAssignedPlayer() != null) continue;

                Image panelImage = panels[i].GetComponent<Image>();
                if (panelImage == null || panelImage.sprite == null) continue;

                if (panelImage.sprite == playerSprite.sprite)
                {
                    panels[i].AssignPlayer(player);
                    panels[i].Show();
                    Debug.Log($"UI Assignment: {player.GetPlayerName()} (Sprite: {playerSprite.sprite.name}) → Panel {i + 1}");
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                Debug.LogWarning($"No matching UI panel found for {player.GetPlayerName()} with sprite {playerSprite.sprite.name}");

                // Fallback: assign to first free panel
                for (int i = 0; i < panels.Length; i++)
                {
                    if (panels[i] != null && panels[i].GetAssignedPlayer() == null)
                    {
                        panels[i].AssignPlayer(player);
                        panels[i].Show();
                        Debug.Log($"UI Assignment (Fallback): {player.GetPlayerName()} → Panel {i + 1}");
                        break;
                    }
                }
            }
        }

        // Initial display update for all visible panels
        foreach (var player in activePlayers)
        {
            UiManager.Instance?.UpdatePlayerUI(player);
        }

        Debug.Log($"UI Assignment complete: {activePlayers.Count} player(s) → panels assigned by sprite.");
    }

    /// <summary>
    /// eliminates player who has run out of hydration
    /// removes them from the game, hides their UI panel and destroys their GameObject
    /// </summary>
    public void EliminatePlayer(Player player)
    {
        if (!players.Contains(player)) return;

        Debug.Log($"{player.GetPlayerName()} is eliminated!");

        // deactivate UI
        if (UiManager.Instance != null)
        {
            UiManager.Instance.SetEventText($"{player.GetPlayerName()} has been eliminated!");
        }

        PlayerUiPanel[] panels = FindObjectsByType<PlayerUiPanel>(FindObjectsSortMode.None);
        foreach (var panel in panels)
        {
            if (panel.IsAssignedTo(player))
            {
                panel.Hide();
                break;
            }
        }

        // BD-call for disqualified layers
        if (DBManager.Instance != null)
        {
            DBManager.Instance.UpdatePlayerStats(
                player.GetPlayerName(),
                roundCount,
                GameResult.Loss,
                GetPlaytimeSeconds()
            );
        }

        // call NextPlayer() first, if it is this players turn
        bool wasCurrentPlayer = players[currentPlayerIndex] == player;

        players.Remove(player);

        if (players.Count == 0)
        {
            Debug.Log("All players eliminated - Game Over!");
            Destroy(player.gameObject);

            // deactivate all buttons
            CardButtonController controller = FindFirstObjectByType<CardButtonController>();
            if (controller != null)
                controller.SetCardButtonsInteractable(false, false);

            // TODO: Game Over Screen?
            return;
        }

        if (wasCurrentPlayer)
        {
            currentPlayerIndex = currentPlayerIndex % players.Count;
            playerMovedThisTurn = false;
            OnNextPlayer?.Invoke();
            UpdateClickableFields();
        }
        else if (currentPlayerIndex >= players.Count)
        {
            currentPlayerIndex = 0;
        }

        // eliminate Player from scene
        Destroy(player.gameObject);
    }

    #region Database Helpers

    /// <summary>
    /// returns the number of rounds played so far
    /// </summary>
    public int GetRoundCount() => roundCount;

    /// <summary>
    /// returns the playtime in seconds since game start
    /// </summary>
    public int GetPlaytimeSeconds()
    {
        return Mathf.RoundToInt(Time.time - gameStartTime);
    }

    #endregion
}