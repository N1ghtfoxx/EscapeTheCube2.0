using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats
{
    public string PlayerName = "";
    public GameResult Result = GameResult.None;
    public int Rounds = 0;
    public int PlaytimeSeconds = 0;
}

public class GameOverScreenManager : MonoBehaviour
{
    public static GameOverScreenManager Instance { get; private set; }

    [SerializeField] private GameObject all;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private GameObject statsListGameObject;
    [SerializeField] private GameObject playerStatsPrefab;

     private void Start()
     {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
            
     }

     public void ShowEndScreen(Player winner, List<PlayerStats> playerStats)
     {
         winnerText.text = winner != null ? $"{winner.GetPlayerName()} wins!" : "Everybody lost!";

        all.SetActive(true);
         foreach (var ps in playerStats)
         {
             GameObject entry = Instantiate(playerStatsPrefab, statsListGameObject.transform);
             TextMeshProUGUI[] entryText = entry.GetComponentsInChildren<TextMeshProUGUI>();

             if (entryText != null && entryText.Length > 0)
             {
                 entryText[0].text = ps.PlayerName;
                 entryText[1].text = ps.Result.ToString();
                 entryText[2].text = ps.Rounds.ToString();
                 entryText[3].text = ps.PlaytimeSeconds.ToString();
             }
             else
             {
                 Debug.LogWarning($"no entry texts {entry.name}");
             }

         }
         
     }

     public void ToPlayerSelection()
     {
         SceneManager.LoadScene("StartScreen");
     }

}
