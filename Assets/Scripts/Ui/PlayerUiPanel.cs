// made by Naomi in collaboration with Claude Ai

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD panel that displays stats for one assigned player
///
/// Assign a player via AssignPlayer() — typically called by GameManager during initialisation
/// The panel then mirrors the player's current hydration, hint cards, and access cards whenever UpdateDisplay() is called
/// </summary>
public class PlayerUiPanel : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector — Stat Labels
    // -------------------------------------------------------------------------

    [SerializeField] private TextMeshProUGUI hydrationText;
    [SerializeField] private TextMeshProUGUI cluesText;
    [SerializeField] private TextMeshProUGUI keycardsText;

    // -------------------------------------------------------------------------
    // /*Inspector — */ Player Identification /*(optional)*/
    // -------------------------------------------------------------------------

    //[Header("Player Identification (Optional)")]
    /*[SerializeField] */private TextMeshProUGUI playerNameText;
    /*[SerializeField] */private Image playerIcon;
    /*[SerializeField] */private Image colorIndicator;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private Player assignedPlayer;

    // -------------------------------------------------------------------------
    // Assignment
    // -------------------------------------------------------------------------

    /// <summary>
    /// Binds this panel to a player and immediately syncs visuals and stats
    /// Pass null to clear the assignment
    /// </summary>
    public void AssignPlayer(Player p)
    {
        assignedPlayer = p;

        if (p != null)
            SyncPlayerVisuals(p);

        UpdateDisplay();
    }

    /// <summary>
    /// Applies the player's name, sprite, and colour to the identification elements of the UI
    /// Called once during AssignPlayer()
    /// </summary>
    private void SyncPlayerVisuals(Player player)
    {
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();

        if (playerNameText != null)
        {
            playerNameText.text = player.GetPlayerName();
        }

        // Player icon — mirrors the actual character sprite
        Image iconImage = playerIcon ?? GetComponent<Image>();
        if (iconImage != null && playerSprite != null)
        {
            iconImage.sprite = playerSprite.sprite;
            iconImage.color = playerSprite.color;
        }
    }

    // -------------------------------------------------------------------------
    // Display Update
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads the assigned player's current stats and writes them to the HUD labels
    /// Called by UiManager.UpdatePlayerUI() whenever the player's state changes
    /// </summary>
    public void UpdateDisplay()
    {
        if (assignedPlayer == null) return;

        if (hydrationText != null) hydrationText.text = assignedPlayer.GetHydration().ToString();
        if (cluesText != null) cluesText.text = assignedPlayer.GetHintCards().ToString();
        if (keycardsText != null) keycardsText.text = assignedPlayer.GetAccessCards().ToString();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // Returns true if this panel is bound to the given player
    public bool IsAssignedTo(Player p) => assignedPlayer == p;

    // Returns the player currently assigned to this panel
    public Player GetAssignedPlayer() => assignedPlayer;

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}