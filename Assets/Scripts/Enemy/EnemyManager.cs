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
    private List<Field> theFields;

    private EnemyType enemyToTeleport;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
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
        
    }

    public void RollForTeleport(EnemyType et)
    {
        enemyToTeleport = et;
        Debug.Log("enemyToTeleport: " + enemyToTeleport);
        if (enemyToTeleport == EnemyType.Bertha)
        {
            EnemyBertha bärtha = enemies[1].GetComponent<EnemyBertha>();
            if (bärtha.isActive)
            {
                Debug.Log("Bertha is already active, cannot teleport.");
                return;
            }
            bärtha.isActive = true;
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
        enemies[0].gameObject.transform.position = new Vector3(20, 20, 0); // move Alf to a temporary position to avoid overlap during the swap
        currPlayer.SetCurrentField(tmp);
        currPlayer.GetCurrentField().OnPlayerArrived(currPlayer);
        GameManager.Instance.UpdateClickableFields();
        enemies[0].CurrentField = tmpP;
    }
    
}

public enum EnemyType
{
    Alf,
    Bertha
}