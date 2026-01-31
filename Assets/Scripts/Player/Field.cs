using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Field : MonoBehaviour
{
    // list of neighboring fields that players can move to
    [SerializeField] protected List<Field> neighbours = new List<Field>();

    protected bool isClickable = false;

    protected virtual void Update()
    {
       // check if mouse button was pressed this frame
       if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
       {
            CheckIfClicked();
       }
    }

    // checks if mouse is over this field when clicked
    private void CheckIfClicked()
    {
        if (!isClickable) return;

        // get mouse position in world space
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // check if mouse is over this station's collider
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePosition);

        if (hitCollider != null && hitCollider.gameObject == gameObject)
        {
            OnFieldClicked();
        }
    }

    // sets wether this field can be clicked
    public void SetClickable(bool clickable)
    {
        isClickable = clickable;
    }

    // handles what happens when this field is clicked
    protected virtual void OnFieldClicked()
    {
        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();
        currentPlayer.MoveToField(this);
    }

    // returns all neighboring fields
    public List<Field> GetNeighbours()
    {
        return neighbours;
    }

    // adds a neighbour to this station
    public void AddNeighbour(Field neighbour)
    {
        if(!neighbours.Contains(neighbour))
        {
            neighbours.Add(neighbour);
        }
    }

    // called when a player arrives at this field
    // - can be overridden by inherited classes
    public virtual void OnPlayerArrived(Player player)
    {
        // empty by default - child classes can override this
    }
}
