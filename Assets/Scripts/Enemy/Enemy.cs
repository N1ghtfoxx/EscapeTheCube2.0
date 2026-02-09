using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Field currentField;
    
    public Field CurrentField
    {
        get => currentField;
        set
        {
            currentField = value;
            transform.position = value.transform.position;
        }
    }
    
}
