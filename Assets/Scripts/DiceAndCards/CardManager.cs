using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;

    private List<Card> ItemCardDictionary = new List<Card>();
    private List<Card> ActionCardDictionary = new List<Card>();

    [SerializeField] private Button[] cardButtons;
    [SerializeField] private TextMeshProUGUI eventText;

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

        FillDict();
        
        foreach (Button btn in cardButtons)
        {
            DiceManager.Instance.OnDiceRoll.AddListener(OnDicesRolling);
            DiceManager.Instance.OnDiceResult.AddListener(OnDicesResult);
        }
        
    }

    private void OnDicesRolling()
    {
        SetCardInteractable(false);
    }
    
    private void OnDicesResult(int diceResult)
    {
        SetCardInteractable(true);
    }
    
    private void SetCardInteractable(bool isInteractable)
    {
        foreach (Button btn in cardButtons)
        {
            btn.interactable = isInteractable;
        }
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
    
    private void SetEventText(string text1,string text2)
    {
        if (eventText != null)
        {
            eventText.text = $"\n{text1}\n[{text2}]\n\n";
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
        
        // display card text and effect in event text UI
        SetEventText(drawnCard.cardtext, drawnCard.cardEffect);
        
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
                // Energydrink: nächster Zug verpflichtend
                Debug.Log($"{currPlayer.GetPlayerName()}'s next turn is mandatory.");
                break;

            case CardEffect.SecretPassage:
                // Geheimgang: Direkter Teleport zu benachbarter Theke
                Debug.Log($"{currPlayer.GetPlayerName()} activates secret passage - teleporting to adjacent counter.");
                currPlayer.ActivateSecretPassage();
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
        
        // display card text and effect in event text UI
        SetEventText(drawnCard.cardtext, drawnCard.cardEffect);
        
        switch (drawnCard.cardEffectType)
        {
            case CardEffect.SpawnAlf:
                // SpawnAlf: Alf's position wird ausgewürfelt
                Debug.Log($"Spawning Alf via dice roll...");
                EnemyManager.Instance.RollForTeleport(EnemyType.Alf);
                break;

            case CardEffect.SwapPositionWithAlf:
                // SwapWithAlf: Swap positions with Alf
                Debug.Log($"{currPlayer.GetPlayerName()} swapping position with Alf...");
                EnemyManager.Instance.SwapPlayerWithAlf(currPlayer);
                break;

            case CardEffect.BlockAlf:
                // UteHelps: Blockiere Alf für eine Runde
                Debug.Log($"Alf is blocked for one round...");
                EnemyManager.Instance.BlockAlf();
                break;

            case CardEffect.FreeMoveTowardsExit:
                // Feueralarm: 1 freie bewegung richtung Ausgang
                Debug.Log($"{currPlayer.GetPlayerName()} has one free move towards exit.");
                // TODO: Implement free move towards exit logic
                break;

            case CardEffect.LoseHintCard:
                // RiceNoPommes: Verliere einen Hinweis
                Debug.Log($"{currPlayer.GetPlayerName()} loses a hint card.");
                currPlayer.RemoveHintCard();
                break;

            case CardEffect.CallBerta:
                // AlfCalls4Reinforcement: Berta joined the game
                Debug.Log($"Calling Berta to join the game...");
                EnemyManager.Instance.RollForTeleport(EnemyType.Bertha);
                break;

            case CardEffect.NoHydrationLoss4Everyone:
                // Stromausfall: alle Spieler k�nnen sich ohne Hydrationsverlust bewegen
                Debug.Log($"Power outage! No hydration loss for all players this turn.");
                GameManager.Instance.EnableNoHydrationLossForAllPlayersThisTurn();
                break;

            case CardEffect.PlayerWithMostAccessCardsLosesOne:
                // SpeiseplanAenderung: Spieler mit den meisten Hinweiskarten verliert einen
                // Note: The description says "Hint Cards" but enum is "AccessCards" - following description
                Debug.Log($"Finding player with most hint cards...");
                GameManager.Instance.GetPlayerWithMostHintCards().RemoveHintCard();
                break;

            case CardEffect.Distraction:
                // Ablenkungsmanoever: Alf für eine Runde einfrieren
                Debug.Log($"Distraction effect activated - Alf frozen for one round.");
                EnemyManager.Instance.BlockAlf();   
                break;

            case CardEffect.Hydration4All:
                // HappyHour: Alle Spieler erhalten +2 Hydration
                List<Player> allPlayers = GameManager.Instance.GetAllPlayers();
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
