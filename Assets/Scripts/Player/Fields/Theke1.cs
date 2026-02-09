using UnityEngine;

public class Theke1 : Field
{
    public override void OnPlayerArrived(Player player)
    {
        base.OnPlayerArrived(player);
        Debug.Log($"{player.GetPlayerName()} arrived at Theke (Safe Space). Alf cannot harm you here!");
        /// TODO: Osman implements Alf logic
    }
}