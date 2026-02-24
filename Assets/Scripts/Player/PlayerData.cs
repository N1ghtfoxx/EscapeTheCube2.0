// made by Naomi in collaboration with Claude Ai

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Persistent data container that carries player-selection information
/// from the CharacterSelection scene into the MainScene
///
/// Lifecycle: created once in the CharacterSelection scene, survives scene loads via
/// DontDestroyOnLoad, and is read by GameManager during player spawning
/// </summary>
public class PlayerData : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static PlayerData Instance { get; private set; }

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
        }
    }

    // -------------------------------------------------------------------------
    // Data Structures
    // -------------------------------------------------------------------------

    // All data confirmed by one player slot in the CharacterSelection screen
    public class SelectedPlayerInfo
    {
        public string playerName;

        // Index of the chosen character button (0–3)
        public int characterIndex;

        public Sprite characterSprite;
    }

    // Internal list indexed by slot order; entries may be null for unused slots
    private List<SelectedPlayerInfo> selectedPlayers = new List<SelectedPlayerInfo>();

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stores the confirmed selection for a given slot
    /// Called by StartHubManager when a player finalises their setup
    /// </summary>
    public void SetPlayerSelection(int playerSlot, string name, int characterIndex, Sprite characterSprite)
    {
        // Grow list to accommodate the slot index if necessary
        while (selectedPlayers.Count <= playerSlot)
            selectedPlayers.Add(null);

        selectedPlayers[playerSlot] = new SelectedPlayerInfo
        {
            playerName = name,
            characterIndex = characterIndex,
            characterSprite = characterSprite
        };

        Debug.Log($"[PlayerData] Slot {playerSlot + 1} saved: {name}, Character index {characterIndex}");
    }

    // Returns all fully configured player selections (null entries excluded)
    public List<SelectedPlayerInfo> GetAllPlayerSelections()
    {
        return selectedPlayers.Where(p => p != null).ToList();
    }

    // Returns the selection for a specific slot, or null if the slot is empty or out of range
    public SelectedPlayerInfo GetPlayerSelection(int playerSlot)
    {
        if (playerSlot < 0 || playerSlot >= selectedPlayers.Count)
            return null;

        return selectedPlayers[playerSlot];
    }

    /// <summary>
    /// Returns true if at least one slot has a valid name and sprite assigned
    /// Used by GameManager to verify data is present before spawning players
    /// </summary>
    public bool HasSelection()
    {
        return selectedPlayers.Any(p => p != null &&
            !string.IsNullOrEmpty(p.playerName) &&
            p.characterSprite != null);
    }

    // Returns the number of fully configured player slots
    public int GetPlayerCount()
    {
        return selectedPlayers.Count(p => p != null &&
            !string.IsNullOrEmpty(p.playerName) &&
            p.characterSprite != null);
    }

    /// <summary>
    /// Clears all stored selections
    /// Call this when returning to the main menu so stale data is not carried over
    /// </summary>
    public void ClearSelection()
    {
        selectedPlayers.Clear();
    }
}