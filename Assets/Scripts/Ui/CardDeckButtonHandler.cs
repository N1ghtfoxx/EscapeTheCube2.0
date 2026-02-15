using UnityEngine;

/// <summary>
/// Wrapper component for card deck buttons
/// Connects UI buttons to CardManager while handling turn logic properly
/// Attach this to your Item Card Button and Action Card Button
/// </summary>
public class CardDeckButtonHandler : MonoBehaviour
{
    [Header("Card Type")]
    [Tooltip("Wich type of card does this button draw?")]
    [SerializeField] private CardDeckType deckType;

    public enum CardDeckType
    {
        ItemCard,
        ActionCard
    }

    /// <summary>
    /// Call this method from the Button's OnClick() event in the Inspector
    /// This will:
    /// 1. Draw the appropriate card via CardManager
    /// 2. Apply the card's effects
    /// 3. End the turn via GameManager.OnCardDrawn() (which calls NextPlayer)
    /// </summary>
    public void OnButtonClicked()
    {
        if (CardManager.Instance == null)
        {
            Debug.LogError("[CardDeckButtonHandler] CardManager.Instance is null!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[CardDeckButtonHandler] GameManager.Instance is null!");
            return;
        }

        // draw the appropriate card
        switch (deckType)
        {
            case CardDeckType.ItemCard:
                CardManager.Instance.HandleItemCard();
                break;
            case CardDeckType.ActionCard:
                CardManager.Instance.HandleActionCard();
                break;
        }

        // end the turn (calls NextPlayer)
        GameManager.Instance.OnCardDrawn();
    }
}
