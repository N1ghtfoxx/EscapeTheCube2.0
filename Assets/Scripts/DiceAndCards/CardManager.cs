using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager instance;

    private List<Card> ItemCardDictionary = new List<Card>();
    private List<Card> ActionCardDictionary = new List<Card>();

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

        FillDict();
    }

    private void FillDict()
    {
        Debug.Log("Filling Card Lists...");
        ItemCardDictionary.Clear();
        ActionCardDictionary.Clear();

        RegisterCategory("ScriptableObjects", ItemCardDictionary, CardType.ItemCard);
        RegisterCategory("ScriptableObjects", ActionCardDictionary, CardType.ActionCard);

    }

    // Helper function, taken from Chris Kloninger. Modified to fit our needs.
    private void RegisterCategory(string path, List<Card> categoryList, CardType ct)
    {
        Card[] assets = Resources.LoadAll<Card>(path);

        foreach (var data in assets)
        {
            if (data == null && data.cardType != ct) continue;
            categoryList.Add(data);
        }
    }


    public void DrawCard(CardType ct)
    {
        Debug.Log("Drawing Card for: " + GameManager.Instance.GetCurrentPlayer().GetPlayerName());
        


    }
}
