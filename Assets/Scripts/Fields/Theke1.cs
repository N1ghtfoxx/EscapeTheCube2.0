using UnityEngine;

/// <summary>
/// Theke 1 field - Safe space where Alf cannot harm players
/// Implements ITheke to be found by Secret Passage card
/// </summary>
public class Theke1 : Field, ITheke
{
    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);
        Debug.Log($"{player.GetPlayerName()} arrived at Theke (Safe Space). Alf cannot harm you here!");
    }
}