using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the interactability of Item and Action card deck buttons
/// Rules:
/// - Player moved this turn: Both Item and Action cards available
/// - Player did NOT move: Only Action cards available
/// - Energy Drink effect active: Must move first (no cards available)
/// - During dice rolls: No cards available
/// </summary>
public class CardButtonController : MonoBehaviour
{
    [Header("Card Deck Buttons")]
    [Tooltip("Drag your Item Card Button GameObject here")]
    [SerializeField] private Button itemCardButton;

    [Tooltip("Drag your Action Card Button GameObject here")]
    [SerializeField] private Button actionCardButton;

    private void Start()
    {
        // Subscribe to game events
        if (DiceManager.Instance != null)
        {
            DiceManager.Instance.OnDiceRoll.AddListener(OnDiceRolling);
            DiceManager.Instance.OnDiceResult.AddListener(OnDiceResult);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNextPlayer.AddListener(OnPlayerChanged);
        }

        // CRITICAL: Set initial state explicitly
        // At game start, player has not moved yet, so only Action cards available
        SetCardButtonsInteractable(false, true);
        Debug.Log("[CardButtonController] Initial state set: Item=disabled, Action=enabled");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (DiceManager.Instance != null)
        {
            DiceManager.Instance.OnDiceRoll.RemoveListener(OnDiceRolling);
            DiceManager.Instance.OnDiceResult.RemoveListener(OnDiceResult);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNextPlayer.RemoveListener(OnPlayerChanged);
        }
    }

    /// <summary>
    /// Called when dice start rolling - disable all card buttons
    /// </summary>
    private void OnDiceRolling()
    {
        SetCardButtonsInteractable(false, false);
        Debug.Log("[CardButtonController] Dice rolling - card buttons disabled");
    }

    /// <summary>
    /// Called when dice finish rolling - update button states based on game rules
    /// </summary>
    private void OnDiceResult(int result)
    {
        UpdateCardButtonStates();
        Debug.Log("[CardButtonController] Dice result received - updating card button states");
    }

    /// <summary>
    /// Called when turn switches to next player - reset button states
    /// </summary>
    private void OnPlayerChanged()
    {
        UpdateCardButtonStates();
        Debug.Log("[CardButtonController] Player changed - resetting card button states");
    }

    /// <summary>
    /// Updates card button interactability based on current game state
    /// RULES:
    /// 1. If player has Energy Drink effect (mandatory movement): NO cards available
    /// 2. If player moved this turn: BOTH Item and Action cards available
    /// 3. If player did NOT move: ONLY Action cards available
    /// 4. During dice rolls: NO cards available (handled by OnDiceRolling)
    /// </summary>
    public void UpdateCardButtonStates()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CardButtonController] GameManager.Instance is null!");
            return;
        }

        // Check if Energy Drink effect is active (mandatory movement)
        if (!GameManager.Instance.CanCurrentPlayerSkipMovement())
        {
            // Player MUST move - cannot draw cards yet
            SetCardButtonsInteractable(false, false);
            Debug.Log("[CardButtonController] Energy Drink active - player must move first (no cards available)");
            return;
        }

        // Check if player moved this turn
        bool playerMoved = GameManager.Instance.DidPlayerMoveThisTurn();

        if (playerMoved)
        {
            // Player moved - both card decks available
            SetCardButtonsInteractable(true, true);
            Debug.Log("[CardButtonController] Player moved - both card decks available");
        }
        else
        {
            // Player did NOT move - only action cards available
            SetCardButtonsInteractable(false, true);
            Debug.Log("[CardButtonController] Player did not move - only Action cards available");
        }
    }

    /// <summary>
    /// Sets the interactability of both card buttons
    /// </summary>
    /// <param name="itemInteractable">Should Item card button be clickable?</param>
    /// <param name="actionInteractable">Should Action card button be clickable?</param>
    private void SetCardButtonsInteractable(bool itemInteractable, bool actionInteractable)
    {
        if (itemCardButton != null)
        {
            itemCardButton.interactable = itemInteractable;
        }
        else
        {
            Debug.LogWarning("[CardButtonController] Item Card Button reference is missing!");
        }

        if (actionCardButton != null)
        {
            actionCardButton.interactable = actionInteractable;
        }
        else
        {
            Debug.LogWarning("[CardButtonController] Action Card Button reference is missing!");
        }
    }

    /// <summary>
    /// Public method to manually update button states
    /// Can be called from other scripts if needed
    /// </summary>
    public void RefreshButtonStates()
    {
        UpdateCardButtonStates();
    }
}