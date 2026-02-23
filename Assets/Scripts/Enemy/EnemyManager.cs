using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [SerializeField] private List<Enemy> enemies;
    [SerializeField] private GameObject fields;
    public List<Field> theFields;

    private EnemyType enemyToTeleport;
    private int blockingAlfCounter = 0;
    
    [Header("Debug")]
    [SerializeField] private bool addListeners = false;
    [SerializeField] private bool addHydrationEachRound = false;

    private void Awake()
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
    
    private void Start()
    {
        theFields = fields.GetComponentsInChildren<Field>().ToList();
        var toRemove = theFields.Where(tmp => tmp.name.Equals("Outline") || tmp.name.Equals("Fields")).ToArray();
        
        foreach (var tmp in toRemove)   
        {
            theFields.Remove(tmp);
            Debug.Log("Removed " + tmp.name);
        }
        
        foreach (var tmp in theFields)
        {
            tmp.name = tmp.name.Substring(tmp.name.Length-3,3).Replace("(", "").Replace(")", "");
        }
        
        // sort the list by name casted to int
        theFields = theFields.OrderBy(tmp => int.Parse(tmp.name)).ToList();
        
        
        // print the names of the remaining transforms for debugging
        foreach (var tmp in theFields)
        {
            Debug.Log(tmp.name);    
        }
        
        enemies[0].CurrentField = theFields[7];
        
        GameManager.Instance.OnNextPlayer.AddListener(OnPlayerChange);

        if (!addListeners)
            return;
        GameManager.Instance.OnNextPlayer.AddListener(AddPlayerHydration);
        
    }

    private void OnPlayerChange()
    {
        if (enemies[0].IsBlocked)
        {
            if (blockingAlfCounter == 0)
            {
                UnblockAlf();
                return;
            }
            
            blockingAlfCounter--;
        }

    }

    public void RollForTeleport(EnemyType et)
    {
        enemyToTeleport = et;
        Debug.Log("enemyToTeleport: " + enemyToTeleport);
        if (enemyToTeleport == EnemyType.Bertha)
        {
            EnemyBertha bärtha = enemies[1].GetComponent<EnemyBertha>();
            if (bärtha.IsActive)
            {
                Debug.Log("Bertha is already active, cannot teleport.");
                return;
            }
            bärtha.IsActive = true;
        }
        else if (enemyToTeleport == EnemyType.Alf && enemies[0].IsBlocked)
        {
            Debug.Log("Alf is blocked, cannot teleport.");
            return;
        }
        
        DiceManager.Instance.OnDiceResult.AddListener(TeleportEnemy);
        DiceManager.Instance.RollDice();
    }
    
    private void TeleportEnemy(int fieldNumber)
    {
        int comp = enemyToTeleport.CompareTo(EnemyType.Alf);
        Enemy enem = enemies[comp];
        enem.CurrentField = theFields[fieldNumber-1];     
        DiceManager.Instance.OnDiceResult.RemoveListener(TeleportEnemy);

    }

    public void SwapPlayerWithAlf(Player currPlayer)
    {
        // save the positions of the player and the enemy in temporary variables
        var tmp = enemies[0].CurrentField;
        var tmpP = currPlayer.GetCurrentField();
        
        // swap the positions
        enemies[0].CurrentField = null; // move Alf to a temporary position to avoid overlap during the swap
        currPlayer.SetCurrentField(tmp);
        currPlayer.GetCurrentField().OnPlayerArrived(currPlayer);
        GameManager.Instance.UpdateClickableFields();
        enemies[0].CurrentField = tmpP;
        
    }

    public void CheckIfPlayersAndAlfOnSameField(EnemyType et)
    {
        List<Player> players = GameManager.Instance.GetAllPlayers();
        
        int comp = et.CompareTo(EnemyType.Alf);
        Field ef = enemies[comp].CurrentField;
        
        foreach (var player in players)
        {
            if (player.GetCurrentField() == ef)
            {
                Debug.Log("Player " + player.name + " is on the same field as " + et);
                player.MoveToField(theFields[0]);
                if (et == EnemyType.Alf)
                    player.RemoveHintCard(player.GetHintCards());
                else
                    player.ConsumeAccessCard(player.GetAccessCards());
            }
            else
            {
                Debug.Log("Player " + player.name + " is safe!");
            }
        }
    }
    
    public void BlockAlf()
    {
        enemies[0].IsBlocked = true;
        blockingAlfCounter++;
    }
    
    private void UnblockAlf()
    {
        enemies[0].IsBlocked = false;
        Debug.Log("Unblocked Alf");
    }

    private void AddPlayerHydration()
    {
        if (!addHydrationEachRound)
            return;
        GameManager.Instance.GetCurrentPlayer().ChangeHydration(2);
    }
    
}

public enum EnemyType
{
    Alf,
    Bertha
}