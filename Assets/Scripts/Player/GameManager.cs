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

    [Header("Testing (Remove when StartHub is ready)")]
    [SerializeField] private bool useTestPlayers = false; // Toggle im Inspector
    [SerializeField] private TestPlayerSetup[] testPlayerSetups; // Flexible Spieler-Konfiguration

    [System.Serializable]
    public class TestPlayerSetup
    {
        public string playerName = "TestPlayer";
        public Sprite characterSprite;
        public bool spawn = true; // Toggle pro Spieler
    }

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

        // Spawn player from StartHub selection
        SpawnPlayerFromSelection();

        // Set starting position for spawned player
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
        // TESTING: Check if test mode is enabled
        if (useTestPlayers)
        {
            SpawnTestPlayers();
            return;
        }

        // Check if PlayerData exists and has valid selections
        if (PlayerData.Instance == null || !PlayerData.Instance.HasSelection())
        {
            Debug.LogWarning("No player selection found! Enable 'Use Test Players' in GameManager for testing.");
            return;
        }

        // Check if player prefab is assigned
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned in GameManager!");
            return;
        }

        // Get all player selections
        var allSelections = PlayerData.Instance.GetAllPlayerSelections();

        Debug.Log($"Spawning {allSelections.Count} player(s)");

        // Spawn each player
        for (int i = 0; i < allSelections.Count; i++)
        {
            var selection = allSelections[i];

            // Calculate spawn position (offset if multiple players)
            Vector3 spawnPosition = startField != null ? startField.transform.position : Vector3.zero;

            // Add small offset for multiple players so they don't overlap
            if (allSelections.Count > 1)
            {
                // Verteile Spieler in einem kleinen Kreis statt nur horizontal
                float angle = (360f / allSelections.Count) * i;
                float radius = 0.15f; // Kleinerer Abstand - bleiben auf dem Field
                float offsetX = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float offsetY = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                spawnPosition += new Vector3(offsetX, offsetY, 0);
            }

            // Spawn the player prefab
            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerObject.name = $"Player_{i + 1}_{selection.playerName}"; // Readable name in hierarchy

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

                // Optional: Apply character-specific stats
                if (selection.characterData != null && selection.characterData.startingHydration > 0)
                {
                    // If you want different starting hydration per character
                    // You'd need to add a method like SetStartingHydration(int amount) to Player.cs
                    // playerComponent.SetStartingHydration(selection.characterData.startingHydration);
                }

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

        // Set all players to start field (but keep their spawn positions with offset!)
        if (startField != null)
        {
            foreach (var player in players)
            {
                player.SetCurrentField(startField, moveToPosition: false); // Keep spawn offset!
            }
        }
    }

    #region TESTING ONLY - Remove when StartHub is ready

    /// <summary>
    /// TEMPORÄR: Spawnt Test-Spieler ohne StartHub
    /// LÖSCHE DIESE METHODE wenn StartHub fertig ist!
    /// </summary>
    private void SpawnTestPlayers()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is not assigned in GameManager!");
            return;
        }

        if (testPlayerSetups == null || testPlayerSetups.Length == 0)
        {
            Debug.LogWarning("[TEST MODE] No test player setups configured!");
            return;
        }

        // Zähle wie viele Spieler spawnen sollen
        var playersToSpawn = testPlayerSetups.Where(setup => setup.spawn).ToList();

        Debug.Log($"[TEST MODE] Spawning {playersToSpawn.Count} test player(s)");

        for (int i = 0; i < playersToSpawn.Count; i++)
        {
            var setup = playersToSpawn[i];

            // Calculate spawn position (offset if multiple players)
            Vector3 spawnPosition = startField != null ? startField.transform.position : Vector3.zero;

            if (playersToSpawn.Count > 1)
            {
                // Verteile Spieler in einem kleinen Kreis
                float angle = (360f / playersToSpawn.Count) * i;
                float radius = 0.5f; // Größerer Abstand damit sie sichtbar getrennt sind
                float offsetX = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float offsetY = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                spawnPosition += new Vector3(offsetX, offsetY, 0);
            }

            // Spawn the player prefab
            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerObject.name = $"TestPlayer_{i + 1}_{setup.playerName}";

            // Get Player component and configure it
            Player playerComponent = playerObject.GetComponent<Player>();
            if (playerComponent != null)
            {
                // Set test name
                playerComponent.SetPlayerName(setup.playerName);

                // Apply sprite if assigned
                SpriteRenderer spriteRenderer = playerObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && setup.characterSprite != null)
                {
                    // Sprite zugewiesen - nutze es OHNE Einfärbung
                    spriteRenderer.sprite = setup.characterSprite;
                    spriteRenderer.color = Color.white; // Original-Farben beibehalten

                    Debug.Log($"[TEST] Player {i + 1} ({setup.playerName}) sprite: {setup.characterSprite.name} (Original colors preserved)");
                }
                else if (spriteRenderer != null)
                {
                    // KEIN Sprite - färbe als Fallback ein
                    Color[] fallbackColors = {
                        new Color(1f, 1f, 0f),      // Gelb
                        new Color(0f, 1f, 0f),      // Grün
                        new Color(1f, 0f, 0f),      // Rot
                        new Color(0.5f, 0f, 0.5f)   // Violett
                    };
                    spriteRenderer.color = fallbackColors[i % fallbackColors.Length];
                    Debug.Log($"[TEST] Player {i + 1} ({setup.playerName}) color: {fallbackColors[i % fallbackColors.Length]} (no sprite assigned)");
                }

                // Add to players list
                players.Add(playerComponent);

                Debug.Log($"[TEST] Player {i + 1} spawned: {setup.playerName}");
            }
            else
            {
                Debug.LogError("Spawned player prefab doesn't have Player component!");
                Destroy(playerObject);
            }
        }

        // Set all players to start field (but keep their spawn positions with offset!)
        if (startField != null)
        {
            foreach (var player in players)
            {
                player.SetCurrentField(startField, moveToPosition: false); // Keep spawn offset!
            }
        }
    }

    #endregion

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
        PlayerUiPanel[] panels = playerUIPanels != null && playerUIPanels.Length > 0
            ? playerUIPanels
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
                panel.AssignPlayer(null);
            }
        }

        // SPRITE-BASIERTE ZUWEISUNG
        // Vergleiche das Sprite des Spielers mit dem Sprite im UI-Panel
        foreach (var player in activePlayers)
        {
            SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
            if (playerSprite == null || playerSprite.sprite == null)
            {
                Debug.LogWarning($"Player {player.GetPlayerName()} has no SpriteRenderer or sprite!");
                continue;
            }

            // Finde das Panel mit dem gleichen Sprite
            bool matched = false;
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;

                // Prüfe ob Panel schon belegt
                if (panels[i].GetAssignedPlayer() != null) continue;

                // Hole das Sprite des Panels (P1/P2/P3/P4 Image Component)
                Image panelImage = panels[i].GetComponent<Image>();
                if (panelImage == null || panelImage.sprite == null) continue;

                // Vergleiche Sprites
                if (panelImage.sprite == playerSprite.sprite)
                {
                    // Match gefunden!
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

                // FALLBACK: Weise dem ersten freien Panel zu
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

        // Initial-Update der sichtbaren Panels
        foreach (var player in activePlayers)
        {
            UiManager.Instance?.UpdatePlayerUI(player);
        }

        Debug.Log($"UI-Zuweisung: {activePlayers.Count} Spieler → UI-Panels nach Sprite zugewiesen");
    }
}