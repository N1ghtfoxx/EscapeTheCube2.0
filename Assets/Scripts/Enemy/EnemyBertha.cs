using UnityEngine;

public class EnemyBertha : Enemy
{
    private bool isActive = false;
    private int playerTurnsSinceActivation = 0;
    private int roundsInGame = 0;
    private int maxRoundsInGame = 2;
    
    void Start()
    {
        // TODO:         
        GameManagerOsmanEdit.Instance.OnNextPlayer.AddListener(OnNextPlayer);
    }

    private void OnNextPlayer()
    {
        if (!isActive)
            return;
        
        if (roundsInGame >= maxRoundsInGame)
            IsActive = false;
        
        if (playerTurnsSinceActivation % GameManagerOsmanEdit.Instance.GetAllPlayers().Count == 0)
            roundsInGame++;
        
        playerTurnsSinceActivation++;
    }

    public bool IsActive
    {
        get => isActive;
        set
        {
            isActive = value;

            if (!value)
            {
                CurrentField = null;  
                playerTurnsSinceActivation = 0;
                roundsInGame = 0;
                Debug.Log("Bertha leaves...");
            }
        } 
    }
}
