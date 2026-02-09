using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    [SerializeField] private List<GameObject> enemies;
    [SerializeField] private GameObject fields;
    private List<Transform> theFields;

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
        
        theFields = fields.GetComponentsInChildren<Transform>().ToList();
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
        
        enemies[0].transform.position = theFields[7].position;
        
    }

    public void RollForTeleport(EnemyType et)
    {
        DiceManager.Instance.OnDiceResult.AddListener(TeleportEnemy);
        DiceManager.Instance.RollDice();
        enemyToTeleport = et;
        
    }

    
    private void TeleportEnemy(int fieldNumber)
    {
        int comp = enemyToTeleport.CompareTo(EnemyType.Alf);
        GameObject enem = enemies[comp];
        enem.transform.position = theFields[fieldNumber-1].position;     
        DiceManager.Instance.OnDiceResult.RemoveListener(TeleportEnemy);
        
    }
    
}

public enum EnemyType
{
    Alf,
    Bertha
}