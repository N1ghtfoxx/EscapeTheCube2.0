using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager instance;

    private Dictionary<int, Card> ItemCardDictionary = new Dictionary<int, Card>();
    private Dictionary<int, Card> ActionCardDictionary = new Dictionary<int, Card>();

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }


    public void DrawCard()
    {
        Debug.Log("Drawing Card for: " + GameManager.Instance.GetCurrentPlayer().GetPlayerName());
    
    }
}
