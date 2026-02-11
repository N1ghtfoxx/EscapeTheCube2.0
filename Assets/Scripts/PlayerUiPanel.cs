using UnityEngine;
using TMPro;

public class PlayerUiPanel: MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hydrationText;
    [SerializeField] private TextMeshProUGUI cluesText;
    [SerializeField] private TextMeshProUGUI keycardsText;

    private Player assignedPlayer;

    public void AssignPlayer(Player p)
    {
        assignedPlayer = p;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (assignedPlayer == null) return;

        if (hydrationText != null) hydrationText.text = assignedPlayer.GetHydration().ToString();
        if (cluesText != null) cluesText.text = assignedPlayer.GetHintCards().ToString();
        if (keycardsText != null) keycardsText.text = assignedPlayer.GetAccessCards().ToString();
    }

    public bool IsAssignedTo(Player p)
    {
        return assignedPlayer == p;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public Player GetAssignedPlayer() => assignedPlayer;
}