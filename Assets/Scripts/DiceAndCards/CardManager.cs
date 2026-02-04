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
            if (data == null || data.cardType != ct) continue;
            categoryList.Add(data);
        }
    }


    public void DrawCard(CardType ct)
    {
        Player currPlayer = GameManager.Instance.GetCurrentPlayer();
        Debug.Log("Drawing Card for: " + currPlayer.GetPlayerName());

        switch (ct)
        {
            case CardType.ItemCard:
                {
                    HandleItemCard();
                    break;
                }
            case CardType.ActionCard:
                {
                    HandleActionCard();
                    break;
                }
                    
        }


    }

    public void HandleItemCard()
    {
        Player currPlayer = GameManager.Instance.GetCurrentPlayer();
        int randomIndex = Random.Range(0, ItemCardDictionary.Count);
        Card drawnCard = ItemCardDictionary[randomIndex];
        Debug.Log($"{currPlayer.GetPlayerName()} drew Item Card: {drawnCard}");

        // Apply hydration (handles both positive and negative values)
        if (drawnCard.hydration != 0)
        {
            currPlayer.ChangeHydration(drawnCard.hydration);
            Debug.Log($"{currPlayer.GetPlayerName()}'s hydration changed by {drawnCard.hydration}. Current hydration: {currPlayer.GetHydration()}/{currPlayer.GetMaxHydration()}");
        }
        
        switch (drawnCard.cardEffectType)
        {
            case CardEffect.HintCard:
                Debug.Log($"{currPlayer.GetPlayerName()} received a hint card.");
                // TODO: Track hint cards in Player system
                break;

            case CardEffect.AccesCard:
                Debug.Log($"{currPlayer.GetPlayerName()} received an access card.");
                // TODO: Track access cards in Player system
                break;

            case CardEffect.NextTurnMandatory:
                Debug.Log($"{currPlayer.GetPlayerName()}'s next turn is mandatory.");
                // TODO: Implement mandatory turn in Player system
                break;

            case CardEffect.None:
                Debug.Log($"Card has no additional effect.");
                break;

            default:
                Debug.Log("No valid item card effect found.");
                break;
        }
    }

    public void HandleActionCard()
    {
        Player currPlayer = GameManager.Instance.GetCurrentPlayer();
        int randomIndex = Random.Range(0, ActionCardDictionary.Count);
        Card drawnCard = ActionCardDictionary[randomIndex];
        Debug.Log($"{currPlayer.GetPlayerName()} drew Action Card: {drawnCard}");
        
        switch (drawnCard.cardEffectType)
        {
            case CardEffect.SpawnAlf:
                Debug.Log($"Spawning Alf via dice roll...");
                DiceManager.Instance.RollDice();
                break;

            case CardEffect.SwapPositionWithAlf:
                Debug.Log($"{currPlayer.GetPlayerName()} swapping position with Alf...");
                // TODO: Implement position swapping logic
                break;

            case CardEffect.BlockAlf:
                Debug.Log($"Blocking Alf for one round...");
                // TODO: Implement Alf blocking
                break;

            case CardEffect.FreeMoveTowardsExit:
                Debug.Log($"{currPlayer.GetPlayerName()} can move freely towards exit.");
                // TODO: Implement free move logic
                break;

            case CardEffect.LoseHintCard:
                Debug.Log($"{currPlayer.GetPlayerName()} lost a hint card.");
                // TODO: Remove hint card from Player
                break;

            case CardEffect.CallBerta:
                Debug.Log($"Calling Berta to join the game...");
                // TODO: Implement calling Berta
                break;

            case CardEffect.NoHydrationLoss4Everyone:
                Debug.Log($"No hydration loss for everyone this turn.");
                // TODO: Implement no hydration loss for all players this turn
                break;

            case CardEffect.PlayerWithMostAccessCardsLosesOne:
                List<Player> allPlayers = GameManager.Instance.GetAllPlayers();
                Debug.Log($"Finding player with most access cards...");
                // TODO: Track access cards and remove one from player with most
                break;

            case CardEffect.Distraction:
                Debug.Log($"Distraction effect activated - Alf frozen for one round.");
                // TODO: Implement distraction effect (likely freezes Alf or affects movement)
                break;

            case CardEffect.SecretPassage:
                Debug.Log($"Secret passage revealed - player can teleport to adjacent counter.");
                // TODO: Implement secret passage teleportation
                break;

            case CardEffect.Hydration4All:
                allPlayers = GameManager.Instance.GetAllPlayers();
                foreach (Player player in allPlayers)
                {
                    if (drawnCard.hydration != 0)
                    {
                        player.ChangeHydration(drawnCard.hydration);
                        Debug.Log($"{player.GetPlayerName()} received {drawnCard.hydration} hydration.");
                    }
                }
                break;

            case CardEffect.None:
                Debug.Log($"Card has no effect.");
                break;

            default:
                Debug.Log("No valid action card effect found.");
                break;
        }
    }
}
