// made by Naomi in collaboration with Claude Ai

using UnityEngine;

/// <summary>
/// Theke 2 — standard counter field with no special rules
/// Implements ITheke so the Secret Passage card can locate it via the marker interface
///
/// Note: Unlike Theke1, this field offers no protection from Alf
/// Override OnPlayerArrived() here if field-specific behaviour is added later
/// </summary>
public class Theke2 : Field, ITheke
{
    // No special behaviour — plain ITheke marker implementation.
}