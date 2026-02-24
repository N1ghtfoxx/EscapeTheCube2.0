// made by Naomi in collaboration with Claude Ai

using UnityEngine;

/// <summary>
/// Connects a card-deck UI button to the card draw and turn-end pipeline
///
/// Attach this component to your Item Card Button and Event Card Button
/// Wire each button's OnClick() event to OnButtonClicked() in the Inspector
///
/// Click flow:
///   1. The correct card type is drawn via CardManager
///   2. The card's effects are applied
///   3. The current turn ends via GameManager.OnCardDrawn() -> NextPlayer()
/// </summary>
public class CardDeckButtonHandler : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Card Type")]
    [Tooltip("Which type of card does this button draw?")]
    [SerializeField] private CardDeckType deckType;

    // -------------------------------------------------------------------------
    // Types
    // -------------------------------------------------------------------------

    public enum CardDeckType
    {
        ItemCard,
        ActionCard
    }

    // -------------------------------------------------------------------------
    // Button Callback
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the Button's OnClick() event in the Inspector
    /// Draws the appropriate card and ends the current player's turn
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

        // Draw the card for the configured deck type
        switch (deckType)
        {
            case CardDeckType.ItemCard:
                CardManager.Instance.HandleItemCard();
                break;
            case CardDeckType.ActionCard:
                CardManager.Instance.HandleActionCard();
                break;
        }

        // End the turn — internally calls NextPlayer()
        GameManager.Instance.OnCardDrawn();
    }
}