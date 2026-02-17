using System;
using UnityEngine;
using UnityEngine.Networking;

// Made with A.I. (Kimi K2.5)
public class DBManager : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost/EscapeTheCube";
    
    public static DBManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Aktualisiert Spieler-Stats (additiv). 
    /// Spieler wird automatisch erstellt falls nicht vorhanden.
    /// </summary>
    /// <param name="playername">Name des Spielers</param>
    /// <param name="rounds">Runden zum Addieren</param>
    /// <param name="result">Win, Loss oder None</param>
    /// <param name="playtimeSeconds">Sekunden zum Addieren</param>
    public void UpdatePlayerStats(string playername, int rounds, GameResult result, int playtimeSeconds)
    {
        StartCoroutine(SendUpdate(playername, rounds, result, playtimeSeconds));
    }

    private System.Collections.IEnumerator SendUpdate(string playername, int rounds, GameResult result, int playtimeSeconds)
    {
        string url = $"{serverUrl}/api/update.php";
        
        string resultStr = result == GameResult.Win ? "win" : (result == GameResult.Loss ? "loss" : "");
        
        string json = $"{{\"playername\":\"{playername}\",\"rounds_played\":{rounds},\"result\":\"{resultStr}\",\"playtime_seconds\":{playtimeSeconds}}}";
        
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            
            yield return req.SendWebRequest();
            
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"DBManager Fehler: {req.error}");
            }
        }
    }
}

public enum GameResult
{
    None,
    Win,
    Loss
}