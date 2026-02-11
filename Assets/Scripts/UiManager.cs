using UnityEngine;
using TMPro;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI eventText;

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

    public void SetEventText(string message)
    {
        if (eventText != null)
        {
            eventText.text = message;
        }
    }

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
