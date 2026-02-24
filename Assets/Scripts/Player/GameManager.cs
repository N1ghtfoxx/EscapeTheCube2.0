// made by Naomi in collaboration with Claude Ai

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Central game loop controller.
///
/// Responsibilities:
///   - Spawning player tokens from StartHub selection data
///   - Managing turn order and round counting
///   - Controlling field clickability and the game-lock mechanism
///   - Handling Power Outage and Energy Drink card effects
///   - Eliminating players who run out of hydration
///   - Triggering the Game Over screen on win or full elimination
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Serialized Fields

    // Single prefab used for every player token
    [SerializeField] private GameObject playerPrefab;

    // The board field all players start on
    [SerializeField] private Field startField;

    [Header("UI Panels")]
    // PlayerUiPanel array, ordered Player 1–4. Assign in Inspector
    [SerializeField] private PlayerUiPanel[] playerUIPanels;

    #endregion

    #region Private Fields

    // Active players (populated at runtime from PlayerData)
    private List<Player> players = new List<Player>();

    // Stats snapshot for each eliminated player (used in the end screen)
    private List<PlayerStats> allEliminatedPlayers = new List<PlayerStats>();

    private List<Field> allFields = new List<Field>();
    private int currentPlayerIndex = 0;

    // Power Outage effect 
    private bool isPowerOutageActive = false;
    private int powerOutageRemainingPlayers = 0;


    private bool playerMovedThisTurn = false;

    private bool isGameLocked = false;

    // Statistics tracked for the database
    private int roundCount;
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
        // Collect all Field components present in the scene
        allFields = FindObjectsByType<Field>(FindObjectsSortMode.None).ToList();

        gameStartTime = Time.time;
        roundCount = 1;

        SpawnPlayersFromSelection();

        // Place the first player on the start field
        if (players.Count > 0 && startField != null)
            players[0].SetCurrentField(startField);

        UpdateClickableFields();
        AssignPlayersToUIPanels();
    }

    /// <summary>
    /// Instantiates all player tokens from the selection confirmed in the StartHub scene
    /// Applies each player's chosen name and character sprite, then places all tokens
    /// on the start field in a correctly repositioned row
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
            Vector3 spawnPosition = startField != null ? startField.transform.position : Vector3.zero;

            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerObject.name = $"Player_{i + 1}_{selection.playerName}";

            Player playerComponent = playerObject.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.SetPlayerName(selection.playerName);

                // Apply the character sprite chosen in the StartHub
                SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && selection.characterSprite != null)
                {
                    spriteRenderer.sprite = selection.characterSprite;
                    spriteRenderer.color = Color.white; // Preserve the sprite's original colours
                    Debug.Log($"[GameManager] Player {i + 1} sprite applied: {selection.characterSprite.name}");
                }

                players.Add(playerComponent);
                Debug.Log($"[GameManager] Player {i + 1} spawned: {selection.playerName}");
            }
            else
            {
                Debug.LogError("[GameManager] Spawned player prefab has no Player component!");
                Destroy(playerObject);
            }
        }

        // All players are registered — place them on the start field with proper row repositioning
        // SetCurrentField(moveToPosition: true) triggers RepositionPlayersOnField(),
        // which centres the full group on the field
        if (startField != null)
        {
            foreach (var player in players)
                player.SetCurrentField(startField, moveToPosition: true);
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

    // Returns the player whose turn is currently active
    public Player GetCurrentPlayer() => players[currentPlayerIndex];

    // Returns all players still in the game
    public List<Player> GetAllPlayers() => players;

    /// <summary>
    /// Advances the turn to the next player.
    ///
    /// Steps performed in order:
    ///   1. Increment the player index (wrapping back to 0 triggers a new round)
    ///   2. Reset the movement flag for the incoming player
    ///   3. Tick down the Power Outage counter
    ///   4. Fire the OnNextPlayer event (CardButtonController, etc. subscribe here)
    ///   5. Refresh clickable fields for the new player's position
    /// </summary>
    public void NextPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        if (currentPlayerIndex == 0)
        {
            roundCount++;
            Debug.Log($"Round {roundCount} started.");
        }

        // Reset BEFORE firing the event so subscribers see the correct state immediately
        playerMovedThisTurn = false;

        // Tick down the Power Outage duration
        if (isPowerOutageActive)
        {
            powerOutageRemainingPlayers--;
            Debug.Log($"Power Outage: {powerOutageRemainingPlayers} player(s) remaining in this round.");

            if (powerOutageRemainingPlayers <= 0)
            {
                isPowerOutageActive = false;
                Debug.Log("Power Outage ended – normal hydration loss resumes.");
            }
        }

        // Fire after resetting the flag so CardButtonController reads the correct state
        OnNextPlayer?.Invoke();

        UpdateClickableFields();
    }

    #endregion

    #region Field Management

    /// <summary>
    /// Enables clicks only on the neighbouring fields of the current player's position
    /// Does nothing while the game is locked (e.g. during a dice roll)
    /// </summary>
    public void UpdateClickableFields()
    {
        if (isGameLocked)
        {
            Debug.Log("Game is locked – fields remain non-clickable");
            return;
        }

        // Reset every field first
        foreach (Field field in allFields)
            field.SetClickable(false);

        // Enable only the neighbours reachable from the current player's field
        Player currentPlayer = GetCurrentPlayer();
        Field currentField = currentPlayer.GetCurrentField();

        if (currentField != null)
        {
            foreach (Field neighbour in currentField.GetNeighbours())
                neighbour.SetClickable(true);
        }
    }

    /// <summary>
    /// Disables all field clicks immediately
    /// Called by Player.MoveToField() to prevent a second move in the same turn
    /// Fields will be re-enabled at the start of the next player's turn
    /// </summary>
    public void DisableAllFields()
    {
        foreach (Field field in allFields)
            field.SetClickable(false);

        Debug.Log("All fields disabled – player can now only draw cards");
    }

    /// <summary>
    /// Returns all fields that currently have at least one player standing on them
    /// Each field is included at most once, even if multiple players share it
    /// </summary>
    public List<Field> GetOccupiedFields()
    {
        List<Field> occupied = new List<Field>();
        foreach (Player player in players)
        {
            Field field = player.GetCurrentField();
            if (field != null && !occupied.Contains(field))
                occupied.Add(field);
        }
        return occupied;
    }

    #endregion

    #region Special Field Actions

    /// <summary>
    /// Checks whether the player can enter the Bistro using an access card
    /// If so, the card is consumed. Called from Bistro.cs.
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
            Debug.Log($"{player.GetPlayerName()} cannot enter Bistro without an access card.");
        }
    }

    #endregion

    #region Card Effects — Power Outage

    /// <summary>
    /// Activates the Power Outage effect (triggered by CardManager)
    /// For one complete round, no player loses hydration when moving
    /// The counter includes the current player (+1) so they also benefit
    /// </summary>
    public void EnableNoHydrationLossForAllPlayersThisTurn()
    {
        isPowerOutageActive = true;
        powerOutageRemainingPlayers = players.Count + 1;

        Debug.Log($"Power Outage activated! No hydration loss for the next {powerOutageRemainingPlayers} moves.");
    }

    /// <summary>
    /// Returns true while the Power Outage effect is active
    /// Queried by Player.MoveToField() before applying hydration loss
    /// </summary>
    public bool IsPowerOutageActive() => isPowerOutageActive;

    #endregion

    #region Card Effects — Movement Tracking

    /// <summary>
    /// Records whether the current player has moved this turn
    /// Called by Player.MoveToField(). Triggers a card-button state refresh
    /// </summary>
    public void SetPlayerMoved(bool moved)
    {
        playerMovedThisTurn = moved;
        UpdateCardButtons();
    }

    /// <summary>
    /// Returns true if the current player has already moved this turn
    /// Used by CardButtonController to decide which card decks to enable
    /// </summary>
    public bool DidPlayerMoveThisTurn() => playerMovedThisTurn;

    /// <summary>Finds CardButtonController in the scene and triggers a state refresh</summary>
    private void UpdateCardButtons()
    {
        CardButtonController controller = FindFirstObjectByType<CardButtonController>();
        if (controller != null)
            controller.UpdateCardButtonStates();
    }

    #endregion

    #region Card Effects — Energy Drink

    /// <summary>
    /// Returns false if the Energy Drink effect is active (player must move this turn)
    /// Queried by CardButtonController to disable both card buttons until the player moves
    /// </summary>
    public bool CanCurrentPlayerSkipMovement()
    {
        return !GetCurrentPlayer().IsNextTurnMandatory();
    }

    #endregion

    #region Helper Methods for CardManager

    /// <summary>
    /// Returns the player who currently holds the most hint cards
    /// Used by CardManager for certain card effects (e.g. steal/compare)
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
    /// Called after a player draws a card (by CardDeckButtonHandler)
    /// Ends the current turn by calling NextPlayer()
    /// </summary>
    public void OnCardDrawn()
    {
        if (players.Count == 0) return;

        Debug.Log($"{GetCurrentPlayer().GetPlayerName()} drew a card – ending turn.");
        NextPlayer();
    }

    #endregion

    #region Dice Events (Game Lock System)

    /// <summary>
    /// Fires when a dice roll begins (via DiceManager.OnDiceRoll)
    /// Locks the game and disables all field clicks
    /// </summary>
    private void OnDiceRollStarted()
    {
        isGameLocked = true;
        Debug.Log("Dice rolling – game locked!");

        foreach (Field field in allFields)
            field.SetClickable(false);
    }

    /// <summary>
    /// Fires when a dice roll finishes (via DiceManager.OnDiceResult)
    /// Unlocks the game and re-enables clickable fields if the player has not yet moved
    /// </summary>
    private void OnDiceRollFinished(int result)
    {
        Debug.Log($"Dice result: {result} – unlocking game!");
        isGameLocked = false;

        if (!playerMovedThisTurn)
        {
            UpdateClickableFields();
        }
        else
        {
            Debug.Log("Player already moved this turn – fields stay disabled after dice roll.");
        }
    }

    /// <summary>
    /// Returns true while the game is locked (dice rolling, animation, etc.)
    /// Queried by UI systems before allowing interaction
    /// </summary>
    public bool IsGameLocked() => isGameLocked;

    #endregion

    // =========================================================================
    // UI PANEL ASSIGNMENT
    // =========================================================================

    private void AssignPlayersToUIPanels()
    {
        // Prefer the explicitly assigned panel array; fall back to scene search
        PlayerUiPanel[] panels = playerUIPanels != null && playerUIPanels.Length > 0
            ? playerUIPanels
            : FindObjectsByType<PlayerUiPanel>(FindObjectsSortMode.None);

        List<Player> activePlayers = GetAllPlayers();

        if (panels == null || panels.Length == 0)
        {
            Debug.LogError("No PlayerUiPanels found! Assign them in GameManager or add them to the scene.");
            return;
        }

        // Hide and unassign every panel before matching
        foreach (var panel in panels)
        {
            if (panel != null)
            {
                panel.Hide();
                panel.AssignPlayer(null);
            }
        }

        // Match each player to the panel whose Image sprite matches the player's sprite
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
                if (panels[i].GetAssignedPlayer() != null) continue; // already taken

                Image panelImage = panels[i].GetComponent<Image>();
                if (panelImage == null || panelImage.sprite == null) continue;

                if (panelImage.sprite == playerSprite.sprite)
                {
                    panels[i].AssignPlayer(player);
                    panels[i].Show();
                    Debug.Log($"UI Assignment: {player.GetPlayerName()} " +
                              $"(Sprite: {playerSprite.sprite.name}) → Panel {i + 1}");
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                Debug.LogWarning($"No matching UI panel found for {player.GetPlayerName()} " +
                                 $"with sprite {playerSprite.sprite.name}");

                // Fallback: assign to the first available panel
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

        // Trigger an initial display refresh for all assigned panels
        foreach (var player in activePlayers)
            UiManager.Instance?.UpdatePlayerUI(player);

        Debug.Log($"UI Assignment complete: {activePlayers.Count} player(s) → panels assigned by sprite.");
    }

    // =========================================================================
    // PLAYER ELIMINATION
    // =========================================================================

    /// <summary>
    /// Removes a player who has run out of hydration from the game.
    ///
    /// Steps:
    ///   1. Displays an elimination message
    ///   2. Hides the player's UI panel
    ///   3. Records the result in the database
    ///   4. Saves the stats snapshot for the end screen
    ///   5. Removes the player from the active list and destroys their token
    ///   6. If the last player was eliminated, shows the Game Over screen
    ///   7. If the eliminated player was the active one, advances the turn
    /// </summary>
    public void EliminatePlayer(Player player)
    {
        if (!players.Contains(player)) return;

        Debug.Log($"{player.GetPlayerName()} is eliminated!");

        // HUD notification
        if (UiManager.Instance != null)
            UiManager.Instance.SetEventText($"{player.GetPlayerName()} has been eliminated!");

        // Hide the player's UI panel
        PlayerUiPanel[] panels = FindObjectsByType<PlayerUiPanel>(FindObjectsSortMode.None);
        foreach (var panel in panels)
        {
            if (panel.IsAssignedTo(player))
            {
                panel.Hide();
                break;
            }
        }

        // Record the loss in the database
        if (DBManager.Instance != null)
        {
            DBManager.Instance.UpdatePlayerStats(
                player.GetPlayerName(),
                roundCount,
                GameResult.Loss,
                GetPlaytimeSeconds()
            );
        }

        // Save a stats snapshot before removing the player from the list
        PlayerStats eliminatedPlayerStats = new PlayerStats
        {
            PlayerName = player.GetPlayerName(),
            Result = GameResult.Loss,
            Rounds = roundCount,
            PlaytimeSeconds = GetPlaytimeSeconds()
        };
        allEliminatedPlayers.Add(eliminatedPlayerStats);

        bool wasCurrentPlayer = players[currentPlayerIndex] == player;

        players.Remove(player);

        // Last player eliminated — game over with no winner
        if (players.Count == 0)
        {
            Debug.Log("All players eliminated – Game Over!");
            Destroy(player.gameObject);

            // Disable card buttons
            CardButtonController controller = FindFirstObjectByType<CardButtonController>();
            if (controller != null)
                controller.SetCardButtonsInteractable(false, false);

            // The last player's stats are already in allEliminatedPlayers (added above)
            List<PlayerStats> allFinalStats = new List<PlayerStats>(allEliminatedPlayers);

            Debug.Log($"GameOver – Total players tracked: {allFinalStats.Count}");

            if (GameOverScreenManager.Instance != null)
                GameOverScreenManager.Instance.ShowEndScreen(null, allFinalStats);

            return;
        }

        // Advance the turn if the eliminated player was the active one
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

        Destroy(player.gameObject);
    }

    // =========================================================================
    // END-SCREEN HELPERS
    // =========================================================================

    /// <summary>
    /// Builds a combined PlayerStats list for the end screen
    /// Includes all previously eliminated players plus every player still active,
    /// marking the winner (if any) with GameResult.Win and the rest with GameResult.Loss
    /// </summary>
    public List<PlayerStats> GetAllPlayersStats(Player winner = null)
    {
        List<PlayerStats> allStats = new List<PlayerStats>(allEliminatedPlayers);

        foreach (Player p in players)
        {
            GameResult result = (p == winner) ? GameResult.Win : GameResult.Loss;

            allStats.Add(new PlayerStats
            {
                PlayerName = p.GetPlayerName(),
                Result = result,
                Rounds = roundCount,
                PlaytimeSeconds = GetPlaytimeSeconds()
            });
        }

        return allStats;
    }

    #region Database Helpers

    // Returns the number of rounds completed so far
    public int GetRoundCount() => roundCount;

    // Returns the total elapsed playtime in whole seconds
    public int GetPlaytimeSeconds() => Mathf.RoundToInt(Time.time - gameStartTime);

    #endregion
}