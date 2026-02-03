using UnityEngine;
using UnityEngine.Pool;

public enum CardType
{
    ItemCard,
    ActionCard
}

public enum CardEffect
{
    None,
    HintCard,
    AccesCard,
    NextTurnMandatory,
    SpawnAlf,
    SwapPositionWithAlf,
    BlockAlf,
    FreeMoveTowardsExit,
    LoseHintCard,
    CallBerta,
    NoHydrationLoss4Everyone,
    PlayerWithMostAccessCardsLosesOne,
    Distraction,
    SecretPassage,
    Hydration4All
}

[CreateAssetMenu(fileName = "Card", menuName = "!SO/Card", order = 1)]
public class Card : ScriptableObject
{
    [Header("Card Properties")]
    [Tooltip("The type of card")]
    public CardType cardType;
    [Tooltip("A Description or Name fo the card")]
    public string cardtext;
    [Tooltip("A Description for the effect of the card")]
    public string cardEffect;
    
    [Header("Item Card Properties")]
    [Tooltip("The amount of hydration you get from the card")]
    public int hydration;
    [Tooltip("The number of turns the card suspends")]
    public int suspendedTurns;
    [Tooltip("The effect of the card")]
    public CardEffect cardEffectType;


}
