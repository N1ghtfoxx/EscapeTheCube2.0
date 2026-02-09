using System.Collections.Generic;
using System.IO;
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

        RegisterCategory("ScriptableObjects/ItemCards", ItemCardDictionary, CardType.ItemCard);
        RegisterCategory("ScriptableObjects/ActionCards", ActionCardDictionary, CardType.ActionCard);

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
                // HinweisKarte: +1 Hinweiskarte
                Debug.Log($"{currPlayer.GetPlayerName()} received a hint card.");
                break;

            case CardEffect.AccesCard:
                // Zugangkarte: +1 Zugangskarte
                Debug.Log($"{currPlayer.GetPlayerName()} received an access card.");
                break;

            case CardEffect.NextTurnMandatory:
                // Energydrink: n�chster Zug verpflichtend
                Debug.Log($"{currPlayer.GetPlayerName()}'s next turn is mandatory.");
                break;

            case CardEffect.SecretPassage:
                // Geheimgang: Direkter Teleport zu benachbarter Theke
                Debug.Log($"{currPlayer.GetPlayerName()} activates secret passage - teleporting to adjacent counter.");
                // TODO: Implement secret passage teleportation to adjacent counter
                break;

            case CardEffect.None:
                // Cola, LactaseTabletten, GreenBanana, Pommes, Nothing, Rotten Steak, SaltyFood, StoneRice
                // Only hydration changes are applied, no additional effects
                Debug.Log($"Card has no additional effect - only hydration change applied.");
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
                // SpawnAlf: Alf's position wird ausgewürfelt
                Debug.Log($"Spawning Alf via dice roll...");
                Enemy.Instance.RollForTeleport();
                break;

            case CardEffect.SwapPositionWithAlf:
                // SwapWithAlf: Swap positions with Alf
                Debug.Log($"{currPlayer.GetPlayerName()} swapping position with Alf...");
                // TODO: Implement position swapping logic with Alf
                break;

            case CardEffect.BlockAlf:
                // UteHelps: Blockiere Alf für eine Runde
                Debug.Log($"Alf is blocked for one round...");
                // TODO: Implement Alf blocking for one turn
                break;

            case CardEffect.FreeMoveTowardsExit:
                // Feueralarm: 1 freie bewegung richtung Ausgang
                Debug.Log($"{currPlayer.GetPlayerName()} has one free move towards exit.");
                // TODO: Implement free move towards exit logic
                break;

            case CardEffect.LoseHintCard:
                // RiceNoPommes: Verliere einen Hinweis
                Debug.Log($"{currPlayer.GetPlayerName()} loses a hint card.");
                // TODO: Remove hint card from Player
                break;

            case CardEffect.CallBerta:
                // AlfCalls4Reinforcement: Berta joined the game
                Debug.Log($"Calling Berta to join the game...");
                // TODO: Implement calling Berta (new enemy character)
                break;

            case CardEffect.NoHydrationLoss4Everyone:
                // Stromausfall: alle Spieler k�nnen sich ohne Hydrationsverlust bewegen
                Debug.Log($"Power outage! No hydration loss for all players this turn.");
                // TODO: Implement no hydration loss for all players this turn
                break;

            case CardEffect.PlayerWithMostAccessCardsLosesOne:
                // SpeiseplanAenderung: Spieler mit den meisten Hinweiskarten verliert einen
                // Note: The description says "Hint Cards" but enum is "AccessCards" - following description
                List<Player> allPlayers = GameManager.Instance.GetAllPlayers();
                Debug.Log($"Finding player with most hint cards...");
                // TODO: Track hint cards and remove one from player with most hint cards
                break;

            case CardEffect.Distraction:
                // Ablenkungsmanoever: Alf f�r eine Runde einfrieren
                Debug.Log($"Distraction effect activated - Alf frozen for one round.");
                // TODO: Implement distraction effect (freezes Alf for one turn)
                break;

            case CardEffect.Hydration4All:
                // HappyHour: Alle Spieler erhalten +2 Hydration
                allPlayers = GameManager.Instance.GetAllPlayers();
                foreach (Player player in allPlayers)
                {
                    if (drawnCard.hydration != 0)
                    {
                        player.ChangeHydration(drawnCard.hydration);
                        Debug.Log($"{player.GetPlayerName()} received {drawnCard.hydration} hydration from Happy Hour.");
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
