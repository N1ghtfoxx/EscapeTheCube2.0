// made by Naomi in collaboration with Claude Ai 

using UnityEngine;

/// <summary>
/// Theke 1 — safe zone field where Alf cannot harm players
/// Implements ITheke so the Secret Passage card can locate it via the marker interface
/// </summary>
public class Theke1 : Field, ITheke
{
    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);
        Debug.Log($"{player.GetPlayerName()} arrived at Theke (Safe Space). Alf cannot harm you here!");
    }
}