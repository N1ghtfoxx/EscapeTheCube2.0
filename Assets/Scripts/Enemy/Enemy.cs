using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyType enemyType;
    private Field currentField;
    private bool isBlocked = false;
    
    public Field CurrentField
    {
        get => currentField;
        set
        {
            currentField = value;
            if (value != null)
            {
                transform.position = value.transform.position;
                EnemyManager.Instance.CheckIfPlayersAndAlfOnSameField(enemyType);
            }
            else
            {
                transform.position = new Vector3(20, 20, 0);  
                if (enemyType == EnemyType.Bertha)
                {
                    gameObject.GetComponent<EnemyBertha>().IsActive = false;
                }
            }
        }
    }

    public bool IsBlocked
    {
        get => isBlocked;
        set => isBlocked = value;
    }

    [ContextMenu("Debug/SetToPlayersField")]
    public void SetToPlayersField()
    {
        CurrentField = GameManager.Instance.GetCurrentPlayer().GetCurrentField();
    }

}
