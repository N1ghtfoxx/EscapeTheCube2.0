// made by Naomi in collaboration with Claude Ai

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the interactability of the Item and Action (Event) card deck buttons
///
/// Rules:
///   - While a dice roll is in progress, BOTH buttons are always disabled (hard lock)
///   - If the Energy Drink effect is active, BOTH buttons are disabled
///     (player must move before drawing any card)
///   - Once the player has moved this turn, BOTH buttons become available
///   - If the player has not yet moved, only the Action card button is available
///
/// The hard lock flag (isDiceRolling) takes priority over every other check and
/// cannot be overridden by external callers or event order
/// </summary>
public class CardButtonController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Card Deck Buttons")]
    [Tooltip("Drag your Item Card Button GameObject here")]
    [SerializeField] private Button itemCardButton;

    [Tooltip("Drag your Event Card Button GameObject here")]
    [SerializeField] private Button actionCardButton;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    /// <summary>
    /// Hard lock that is true for the entire duration of a dice roll
    /// Prevents OnPlayerChanged or any external call from re-enabling the buttons
    /// while the dice are still in the air
    /// </summary>
    private bool isDiceRolling = false;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Subscribe to dice and turn events
        if (DiceManager.Instance != null)
        {
            DiceManager.Instance.OnDiceRoll.AddListener(OnDiceRolling);
            DiceManager.Instance.OnDiceResult.AddListener(OnDiceResult);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNextPlayer.AddListener(OnPlayerChanged);
        }

        // At game start the player has not yet moved, so only Action cards are available
        SetCardButtonsInteractable(false, true);
        Debug.Log("[CardButtonController] Initial state set: Item=disabled, Action=enabled");
    }

    private void OnDestroy()
    {
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

    // -------------------------------------------------------------------------
    // Event Handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fires when the dice start rolling
    /// Engages the hard lock and disables both buttons immediately
    /// </summary>
    private void OnDiceRolling()
    {
        isDiceRolling = true;
        SetCardButtonsInteractable(false, false);
        Debug.Log("[CardButtonController] Dice rolling – card buttons disabled");
    }

    /// <summary>
    /// Fires when the dice finish rolling
    /// Releases the hard lock, then re-evaluates the correct button state
    /// </summary>
    private void OnDiceResult(int result)
    {
        isDiceRolling = false;
        UpdateCardButtonStates();
        Debug.Log("[CardButtonController] Dice result received – updating card button states");
    }

    /// <summary>
    /// Fires when the turn switches to the next player
    /// Skipped entirely while the dice are rolling to avoid race conditions
    /// (a NextPlayer() call triggered mid-roll must not re-enable the buttons)
    /// </summary>
    private void OnPlayerChanged()
    {
        if (isDiceRolling)
        {
            Debug.Log("[CardButtonController] Player changed DURING dice roll – skipping state update (hard lock active)");
            return;
        }

        UpdateCardButtonStates();
        Debug.Log("[CardButtonController] Player changed – resetting card button states");
    }

    // -------------------------------------------------------------------------
    // State Evaluation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Determines and applies the correct interactability state for both buttons
    /// based on the current game state (dice lock → Energy Drink → movement flag)
    /// </summary>
    public void UpdateCardButtonStates()
    {
        // Hard lock: dice are rolling — nothing else matters
        if (isDiceRolling)
        {
            SetCardButtonsInteractable(false, false);
            Debug.Log("[CardButtonController] UpdateCardButtonStates called during dice roll – hard lock enforced");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CardButtonController] GameManager.Instance is null!");
            return;
        }

        // Energy Drink effect: player must move first — no cards available yet
        if (!GameManager.Instance.CanCurrentPlayerSkipMovement())
        {
            SetCardButtonsInteractable(false, false);
            Debug.Log("[CardButtonController] Energy Drink active – player must move first (no cards available)");
            return;
        }

        // Normal turn logic: both decks available after moving, only Action cards before
        bool playerMoved = GameManager.Instance.DidPlayerMoveThisTurn();

        if (playerMoved)
        {
            SetCardButtonsInteractable(true, true);
            Debug.Log("[CardButtonController] Player moved – both card decks available");
        }
        else
        {
            SetCardButtonsInteractable(false, true);
            Debug.Log("[CardButtonController] Player has not moved – only Action cards available");
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Directly sets the interactability of both card buttons
    /// Called by GameManager (e.g. on full elimination) as well as internally
    /// </summary>
    public void SetCardButtonsInteractable(bool itemInteractable, bool actionInteractable)
    {
        if (itemCardButton != null)
            itemCardButton.interactable = itemInteractable;
        else
            Debug.LogWarning("[CardButtonController] Item Card Button reference is missing!");

        if (actionCardButton != null)
            actionCardButton.interactable = actionInteractable;
        else
            Debug.LogWarning("[CardButtonController] Action Card Button reference is missing!");
    }

    /// <summary>
    /// Forces a full state re-evaluation
    /// Can be called from other scripts if needed
    /// </summary>
    public void RefreshButtonStates()
    {
        UpdateCardButtonStates();
    }
}