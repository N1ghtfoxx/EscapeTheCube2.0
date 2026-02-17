using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Stores player selection data from StartHub.
/// Persists between scene loads using DontDestroyOnLoad.
/// </summary>
public class PlayerData : MonoBehaviour
{
    #region Singleton

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

    #endregion

    #region Player Selection Data

    public class SelectedPlayerInfo
    {
        public string playerName;
        public int characterIndex; // 0-3
        public Sprite characterSprite; // sprite taken directly from the character button
    }

    private List<SelectedPlayerInfo> selectedPlayers = new List<SelectedPlayerInfo>();

    #endregion

    #region Public Methods

    /// <summary>
    /// Called from StartHub when a player confirms their selection.
    /// </summary>
    public void SetPlayerSelection(int playerSlot, string name, int characterIndex, Sprite characterSprite)
    {
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

    /// <summary>
    /// Returns all selected players (null entries filtered out).
    /// </summary>
    public List<SelectedPlayerInfo> GetAllPlayerSelections()
    {
        return selectedPlayers.Where(p => p != null).ToList();
    }

    /// <summary>
    /// Returns the selection for a specific slot, or null.
    /// </summary>
    public SelectedPlayerInfo GetPlayerSelection(int playerSlot)
    {
        if (playerSlot < 0 || playerSlot >= selectedPlayers.Count)
            return null;
        return selectedPlayers[playerSlot];
    }

    /// <summary>
    /// True if at least one player has a valid name and sprite selected.
    /// </summary>
    public bool HasSelection()
    {
        return selectedPlayers.Any(p => p != null &&
            !string.IsNullOrEmpty(p.playerName) &&
            p.characterSprite != null);
    }

    /// <summary>
    /// Number of fully configured players.
    /// </summary>
    public int GetPlayerCount()
    {
        return selectedPlayers.Count(p => p != null &&
            !string.IsNullOrEmpty(p.playerName) &&
            p.characterSprite != null);
    }

    /// <summary>
    /// Clears all selections (e.g. when returning to main menu).
    /// </summary>
    public void ClearSelection()
    {
        selectedPlayers.Clear();
    }

    #endregion
}