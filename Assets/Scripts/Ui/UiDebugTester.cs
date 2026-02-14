using UnityEngine;

/// <summary>
/// DEBUG SCRIPT - Attach to any GameObject to test UI updates
/// Press number keys to test different scenarios
/// REMOVE THIS SCRIPT IN PRODUCTION!
/// </summary>
public class UiDebugTester : MonoBehaviour
{
    private void Update()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[UiDebugTester] GameManager.Instance is null!");
            return;
        }

        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogWarning("[UiDebugTester] No current player found!");
            return;
        }

        // Press 1 - Add Hint Card
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            Debug.Log("[UiDebugTester] Key 1 pressed - Adding hint card");
            currentPlayer.AddHintCard(1);
        }

        // Press 2 - Remove Hint Card
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            Debug.Log("[UiDebugTester] Key 2 pressed - Removing hint card");
            currentPlayer.RemoveHintCard(1);
        }

        // Press 3 - Add Access Card
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            Debug.Log("[UiDebugTester] Key 3 pressed - Adding access card");
            currentPlayer.AddAccessCard(1);
        }

        // Press 4 - Remove Access Card
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            Debug.Log("[UiDebugTester] Key 4 pressed - Removing access card");
            currentPlayer.ConsumeAccessCard(1);
        }

        // Press 5 - Add Hydration
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            Debug.Log("[UiDebugTester] Key 5 pressed - Adding hydration");
            currentPlayer.ChangeHydration(1);
        }

        // Press 6 - Remove Hydration
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
        {
            Debug.Log("[UiDebugTester] Key 6 pressed - Removing hydration");
            currentPlayer.ChangeHydration(-1);
        }

        // Press 0 - Print Debug Info
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            PrintDebugInfo();
        }
    }

    private void PrintDebugInfo()
    {
        Debug.Log("========== UI DEBUG INFO ==========");

        // Check UiManager
        if (UiManager.Instance == null)
        {
            Debug.LogError("UiManager.Instance is NULL!");
        }
        else
        {
            Debug.Log("? UiManager.Instance exists");
        }

        // Check GameManager
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL!");
            return;
        }
        else
        {
            Debug.Log("? GameManager.Instance exists");
        }

        // Check Players
        var players = GameManager.Instance.GetAllPlayers();
        Debug.Log($"Number of players: {players.Count}");
        foreach (var player in players)
        {
            Debug.Log($"  - {player.GetPlayerName()}: Hydration={player.GetHydration()}, Hints={player.GetHintCards()}, Access={player.GetAccessCards()}");
        }

        // Check UI Panels
        PlayerUiPanel[] panels = FindObjectsByType<PlayerUiPanel>(FindObjectsSortMode.None);
        Debug.Log($"Number of UI Panels found: {panels.Length}");
        foreach (var panel in panels)
        {
            if (panel == null)
            {
                Debug.LogWarning("  - Found NULL panel!");
                continue;
            }

            Player assigned = panel.GetAssignedPlayer();
            if (assigned != null)
            {
                Debug.Log($"  - Panel '{panel.gameObject.name}' - {assigned.GetPlayerName()}");
            }
            else
            {
                Debug.LogWarning($"  - Panel '{panel.gameObject.name}' - NO PLAYER ASSIGNED!");
            }
        }

        Debug.Log("===================================");
    }

    private void OnGUI()
    {
        // Show controls on screen
        GUI.Label(new Rect(10, 10, 300, 200),
            "UI DEBUG TESTER\n\n" +
            "1 - Add Hint Card\n" +
            "2 - Remove Hint Card\n" +
            "3 - Add Access Card\n" +
            "4 - Remove Access Card\n" +
            "5 - Add Hydration\n" +
            "6 - Remove Hydration\n" +
            "0 - Print Debug Info");
    }
}