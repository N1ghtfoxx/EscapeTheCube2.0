// made by Naomi in collaboration with Claude Ai

using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Central UI manager for in-game feedback and player panel updates
///
/// Responsibilities:
///   - Displaying queued event messages in the UpperPanel text field
///   - Routing player-stat updates to the correct PlayerUiPanel
///
/// Called by CardManager, GameManager, field scripts, etc.
/// Most Player methods (AddHintCard, ChangeHydration, …) trigger UpdatePlayerUI() automatically,
/// so manual calls are rarely needed
/// </summary>
public class UiManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static UiManager Instance { get; private set; }

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
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    // HUD text element used to display event messages
    [SerializeField] private TextMeshProUGUI eventText;

    [Header("Event Message Settings")]
    [Tooltip("How long (in seconds) each queued message is visible before the next one appears")]
    [SerializeField] private float messageDuration = 2.5f;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private Queue<string> messageQueue = new Queue<string>();
    private bool isDisplayingMessage = false;

    // -------------------------------------------------------------------------
    // Event Messages
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a message to the display queue
    /// If no message is currently shown, the queue starts playing immediately
    /// Each message is visible for <see cref="messageDuration"/> seconds
    /// </summary>
    public void SetEventText(string message)
    {
        if (eventText == null)
        {
            Debug.LogWarning($"[UiManager] eventText reference is missing in Inspector! Cannot display: {message}");
            return;
        }

        messageQueue.Enqueue(message);

        if (!isDisplayingMessage)
        {
            StartCoroutine(DisplayMessageQueue());
        }
    }

    /// <summary>
    /// Coroutine that drains the message queue one entry at a time,
    /// pausing for <see cref="messageDuration"/> between messages
    /// Clears the text field once the queue is empty
    /// </summary>
    private IEnumerator DisplayMessageQueue()
    {
        isDisplayingMessage = true;

        while (messageQueue.Count > 0)
        {
            eventText.text = messageQueue.Dequeue();
            yield return new WaitForSeconds(messageDuration);
        }

        eventText.text = "";
        isDisplayingMessage = false;
    }

    /// <summary>
    /// Immediately stops all queued messages and clears the text field
    /// Useful when resetting UI state (e.g. on game over)
    /// </summary>
    public void ClearEventText()
    {
        StopAllCoroutines();
        messageQueue.Clear();

        if (eventText != null)
            eventText.text = "";

        isDisplayingMessage = false;
    }

    // -------------------------------------------------------------------------
    // Player Panel Updates
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes the HUD panel that belongs to the given player
    /// Searches all active PlayerUiPanel components for a matching assignment
    ///
    /// NOTE: Most Player methods call this automatically — manual calls are rarely needed
    /// </summary>
    public void UpdatePlayerUI(Player player)
    {
        if (player == null) return;

        PlayerUiPanel[] panels = FindObjectsByType<PlayerUiPanel>(FindObjectsSortMode.None);
        foreach (var panel in panels)
        {
            if (panel.IsAssignedTo(player))
            {
                panel.UpdateDisplay();
                return;
            }
        }

        Debug.LogWarning($"[UiManager] No UI panel found for player: {player.GetPlayerName()}");
    }
}