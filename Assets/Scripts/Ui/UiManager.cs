using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI eventText;

    [Header("Event Message Settings")]
    [SerializeField] private float messageDuration = 2.5f;

    private Queue<string> messageQueue = new Queue<string>();
    private bool isDisplayingMessage = false;

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
            return;
        }
    }

    /// <summary>
    /// Displays an event message to the player
    /// Messages are queued and shown one after another
    /// Each message is displayed for messageDuration seconds
    /// Called by CardManager, GameManager, EnemyManager, etc.
    /// </summary>
    public void SetEventText(string message)
    {
        if (eventText == null)
        {
            Debug.LogWarning($"[UiManager] eventText reference is missing in Inspector! Cannot display: {message}");
            return;
        }

        // add message to queue
        messageQueue.Enqueue(message);

        // start message display if not already showing
        if (!isDisplayingMessage)
        {
            StartCoroutine(DisplayMessageQueue());
        }
    }

    /// <summary>
    /// Displays messages from the queue one by one
    /// </summary>
    private IEnumerator DisplayMessageQueue()
    {
        isDisplayingMessage = true;

        while (messageQueue.Count > 0)
        {
            // get next message
            string message = messageQueue.Dequeue();

            // show message
            eventText.text = message;

            // wait for duration
            yield return new WaitForSeconds(messageDuration);
        }

        // clear text after last nessage
        eventText.text = "";
        isDisplayingMessage = false;
    }

    /// <summary>
    /// Clears the message queue and current message immediately
    /// Useful for resetting UI state
    /// </summary>
    public void ClearEventText()
    {
        StopAllCoroutines();
        messageQueue.Clear();
        if (eventText != null)
        {
            eventText.text = "";
        }
        isDisplayingMessage = false;
    }

    /// <summary>
    /// Updates the UI panel for a specific player
    /// Automatically finds the correct PlayerUiPanel assigned to this player
    /// NOTE: Most Player methods (AddHintCard, ChangeHydration, etc.) call this automatically!
    /// You usually don't need to call this manually.
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
        Debug.LogWarning($"Kein UI-Panel für {player.GetPlayerName()} gefunden.");
    }
}
