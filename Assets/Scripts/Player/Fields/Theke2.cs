using UnityEngine;

/// <summary>
/// Theke 2 field - Regular counter field
/// Implements ITheke to be found by Secret Passage card
/// Note: Unlike Theke1, this has no special Alf protection
/// </summary>
public class Theke2 : Field, ITheke
{
    // No special behavior - just a regular field that can be found by Secret Passage
    // Override OnPlayerArrived if needed in the future
}