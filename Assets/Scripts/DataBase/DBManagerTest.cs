using UnityEngine;

// Made with A.I. (Kimi K2.5)
public class DBManagerTest : MonoBehaviour
{
    [Header("Spieler Daten")]
    [SerializeField] private string playername = "TestPlayer";
    
    [SerializeField] private int rounds = 5;
    
    [SerializeField] private GameResult result = GameResult.Win;
    
    [SerializeField] private int playtimeSeconds = 300;

    [ContextMenu("Update Player Stats")]
    private void UpdatePlayerStats()
    {
        if (DBManager.Instance == null)
        {
            Debug.LogError("DBManager nicht gefunden! Bitte DBManager in der Szene platzieren.");
            return;
        }

        Debug.Log($"Sende: {playername} | Runden: {rounds} | Result: {result} | Zeit: {playtimeSeconds}s");
        
        DBManager.Instance.UpdatePlayerStats(playername, rounds, result, playtimeSeconds);
    }
}