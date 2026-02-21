using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the interactability of Item and Action card deck buttons
/// </summary>
public class CardButtonController : MonoBehaviour
{
    [Header("Card Deck Buttons")]
    [Tooltip("Drag your Item Card Button GameObject here")]
    [SerializeField] private Button itemCardButton;

    [Tooltip("Drag your Action Card Button GameObject here")]
    [SerializeField] private Button actionCardButton;

    // hard lock: true while ANY dice roll is in progress
    // prevents OnPlayerChanged or external calls from re-enabling buttons
    private bool isDiceRolling = false;

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
    /// Called when dice start rolling - sets hard lock and disables all card buttons
    /// </summary>
    private void OnDiceRolling()
    {
        isDiceRolling = true;
        SetCardButtonsInteractable(false, false);
        Debug.Log("[CardButtonController] Dice rolling - card buttons disabled");
    }

    /// <summary>
    /// Called when dice finish rolling - releases hard lock, then re-evaluates correct state
    /// </summary>
    private void OnDiceResult(int result)
    {
        isDiceRolling = false;
        UpdateCardButtonStates();
        Debug.Log("[CardButtonController] Dice result received - updating card button states");
    }

    /// <summary>
    /// Called when turn switches to next player 
    /// does nothing while dice are rolling 
    /// without this guard, a NextPlayer() call triggered mid-roll would re-enable action cards 
    /// </summary>
    private void OnPlayerChanged()
    {
        if (isDiceRolling)
        {
            Debug.Log("[CardButtonController] Player changed DURING dice roll - skipping state update (hard lock active)");
            return;
        }

        UpdateCardButtonStates();
        Debug.Log("[CardButtonController] Player changed - resetting card button states");
    }

    /// <summary>
    /// Updates card button interactability based on current game state
    /// Immediately forces disabled state if dice are rolling
    /// </summary>
    public void UpdateCardButtonStates()
    {
        // hard lock while dice are rolling - no other code path can override this
        if (isDiceRolling)
        {
            SetCardButtonsInteractable(false, false);
            Debug.Log("[CardButtonController] UpdateCardButtonStates called during dice roll - hard lock enforced");
            return;
        }

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
    public void SetCardButtonsInteractable(bool itemInteractable, bool actionInteractable)
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