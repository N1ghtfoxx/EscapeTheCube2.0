using UnityEngine;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Stores player selection data from StartHub
/// Persists between scene loads using DontDestroyOnLoad
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

    [System.Serializable]
    public class SelectedPlayerInfo
    {
        public string playerName;
        public int characterIndex; // 0-3 for 4 Chars
        public CharacterData characterData; // reference to the chosen char data
    }

    private List<SelectedPlayerInfo> selectedPlayers = new List<SelectedPlayerInfo>();

    #endregion

    #region Public Methods

    /// <summary>
    /// Called from StartHub when a player makes their selection
    /// </summary>
    public void SetPlayerSelection(int playerSlot, string name, int characterIndex, CharacterData characterData)
    {
        // ensure list is large enough
        while (selectedPlayers.Count <= playerSlot)
        {
            selectedPlayers.Add(null);
        }

        // create or update player info
        selectedPlayers[playerSlot] = new SelectedPlayerInfo
        {
            playerName = name,
            characterIndex = characterIndex,
            characterData = characterData
        };
        Debug.Log($"Player {playerSlot + 1} selection saved: {name}, Character {characterIndex} ({characterData.characterName})");
    }

    /// <summary>
    /// Returns all selected players
    /// </summary>
    public List<SelectedPlayerInfo> GetAllPlayerSelections()
    {
        // filter out null entries
        return selectedPlayers.Where(p => p != null).ToList();
    }

    /// <summary>
    /// Returns specific player selection by slot
    /// </summary>
    public SelectedPlayerInfo GetPlayerSelection(int playerSlot)
    {
        if (playerSlot < 0 || playerSlot >= selectedPlayers.Count)
            return null;

        return selectedPlayers[playerSlot];
    }

    /// <summary>
    /// Checks if at least one player has made a selection
    /// </summary>
    public bool HasSelection()
    {
        return selectedPlayers.Any(p => p != null &&
            !string.IsNullOrEmpty(p.playerName) && 
            p.characterData != null);
    }

    /// <summary>
    /// Returns number of players who made selections
    /// </summary>
    public int GetPlayerCount()
    {
        return selectedPlayers.Count(p => p != null &&
        !string.IsNullOrEmpty(p.playerName) && 
        p.characterData != null);
    }

    /// <summary>
    /// Clears the selection (useful for returning to main menu)
    /// </summary>
    public void ClearSelection()
    {
        selectedPlayers.Clear();
    }

    #endregion
}
