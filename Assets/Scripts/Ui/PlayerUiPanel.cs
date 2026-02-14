using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUiPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hydrationText;
    [SerializeField] private TextMeshProUGUI cluesText;
    [SerializeField] private TextMeshProUGUI keycardsText;

    [Header("Player Identification (Optional)")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image playerIcon;
    [SerializeField] private Image colorIndicator;

    private Player assignedPlayer;

    public void AssignPlayer(Player p)
    {
        assignedPlayer = p;

        // Sync UI visuals with player
        if (p != null)
        {
            SyncPlayerVisuals(p);
        }

        UpdateDisplay();
    }

    /// <summary>
    /// Syncs the UI panel visuals with the assigned player
    /// Shows player name, sprite, and color to identify which player this panel belongs to
    /// </summary>
    private void SyncPlayerVisuals(Player player)
    {
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();

        // Update player name text (colored to match player)
        if (playerNameText != null)
        {
            playerNameText.text = player.GetPlayerName();

            // Color the name to match the player sprite
            if (playerSprite != null)
            {
                playerNameText.color = playerSprite.color;
            }
        }

        // Update player icon (shows the actual sprite)
        Image iconImage = playerIcon;
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        if (iconImage != null && playerSprite != null)
        {
            iconImage.sprite = playerSprite.sprite;
            iconImage.color = playerSprite.color;
        }

        // Update color indicator (background or border)
        if (colorIndicator != null && playerSprite != null)
        {
            colorIndicator.color = playerSprite.color;
        }
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