using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class Enemy : MonoBehaviour
{
    public static Enemy Instance;
    
    [FormerlySerializedAs("theFields")] [SerializeField] private GameObject _Fields;
    private List<Transform> theFields;

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
        
        theFields = _Fields.GetComponentsInChildren<Transform>().ToList();
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
        
    }

    public void RollForTeleport()
    {
        DiceManager.Instance.OnDiceResult.AddListener(TeleportEnemy);
        DiceManager.Instance.RollDice();
        
    }

    private void TeleportEnemy(int fieldNumber)
    {
        transform.position = theFields[fieldNumber-1].position;     
        DiceManager.Instance.OnDiceResult.RemoveListener(TeleportEnemy);
    }
    
}
